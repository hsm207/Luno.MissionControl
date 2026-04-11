# Lesson: The AppHost Background Execution Trap

## The Incident

During the stabilization and testing of the `Luno.MissionControl` UI, we attempted to run the Aspire AppHost in the background using the CLI's native `--background` flag (`aspire start --background`). 

The command would appear to succeed, returning control to the terminal. However, any subsequent attempts to interrogate the AppHost (e.g., `aspire ps`, `aspire describe`) would fail with the error:
`No running apphost found. Use 'aspire run' to start one first.`

This led to a frustrating cycle of "ghost" deployments, port roulette (due to `--isolated` mode), and an inability to obtain the dynamically assigned endpoints needed to run Playwright E2E tests.

## Root Cause Analysis

The failure was a compounding effect of process group management and the behavior of the Aspire CLI's background detachment:

1.  **The Detach-Child Murder**: When `aspire start --background` is invoked, the CLI spawns a detached child process to build and run the AppHost. 
2.  **The Shell Cleanup Executioner**: In automated or agentic shell environments, when the primary command (`aspire start`) completes and the shell session ends its "turn", the operating system (or the execution wrapper) aggressively cleans up the entire process group to prevent orphaned processes.
3.  **The Mid-Build Assassination**: The `detach-child` logs revealed that the background AppHost process was being forcefully terminated (Exit Code 2) right in the middle of the `dotnet build` phase. It wasn't failing to start due to code errors; it was being killed by the environment before it could even boot the application host.

## The Misguided Workarounds

Before discovering the root cause, several incorrect assumptions wasted diagnostic turns:
*   **Assuming Port Conflicts**: We assumed the AppHost was failing due to port conflicts and continually relied on the `--isolated` flag, which only made tracking the (dead) instances harder.
*   **Ignoring the Logs**: We relied on the high-level CLI output ("Started successfully") instead of immediately dumping the `~/.aspire/logs/*detach-child*.log` files, which clearly showed the sudden termination mid-build.

## Actionable Guardrails

1.  **Never Use the CLI `--background` Flag in Ephemeral Shells**: When orchestrating Aspire from an agent, script, or CI/CD environment that aggressively cleans up process groups, do NOT use `aspire start --background`. The detached process will be murdered when the shell exits.
2.  **The "Sleep Infinity" Persistence Pattern**: To run an AppHost in the background reliably within these environments, use standard foreground execution but instruct the shell environment itself to stay alive and backgrounded:
    ```bash
    # Correct way to background in an agentic shell:
    aspire start --apphost <path> && sleep infinity
    # (Execute this command block with the agent's background execution tool/flag)
    ```
3.  **Atomic Startup and Verification**: Never assume an AppHost is running just because the start command exited with code 0. Always chain the startup with a verification command to ensure the resource actually reaches a healthy state before returning control:
    ```bash
    aspire start --apphost <path> && sleep 5 && aspire wait <resource> && aspire describe --format Json
    ```
4.  **Read the Detach Logs First**: If an AppHost "ghosts" you, do not guess. Immediately read the child process logs. They are located outside the workspace in `~/.aspire/logs/`.
    ```bash
    ls -t ~/.aspire/logs/*detach-child*.log | head -n 1 | xargs cat
    ```
