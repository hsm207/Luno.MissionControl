const { chromium } = require('playwright');
const { execSync } = require('child_process');

async function verify() {
    console.log('🚀 Starting production deployment verification...');

    // 1. Find the URL for the webfrontend
    let url = process.argv[2] || process.env.WEB_FRONTEND_URL;
    if (!url) {
        try {
            const containerName = execSync('docker ps --filter "name=webfrontend" --format "{{.Names}}" | head -n 1').toString().trim();
            if (!containerName) throw new Error("Container not found");
            
            const port = execSync(`docker port ${containerName} 8080 | grep -oE "[0-9]+$" | head -n 1`).toString().trim();
            url = `http://localhost:${port}`;
        } catch (e) {
            console.error('❌ Could not find webfrontend container or port. Is the deployment running?');
            process.exit(1);
        }
    }

    console.log(`🌐 Testing frontend at: ${url}`);

    const browser = await chromium.launch();
    const page = await browser.newPage();

    // Listen for console errors
    page.on('console', msg => {
        if (msg.type() === 'error') {
            console.error(`❌ Browser Console Error: ${msg.text()}`);
            // Ignore common non-critical errors like favicon
            if (msg.text().includes('favicon.ico')) return;
            
            if (msg.text().includes('blazor.web.js') || msg.text().includes('_framework') || msg.text().includes('404')) {
                console.error('🚨 STATIC ASSET FAILURE DETECTED!');
                process.exit(1);
            }
        }
    });

    const maxRetries = 5;
    let attempt = 1;
    let success = false;

    while (attempt <= maxRetries && !success) {
        try {
            console.log(`   Attempt ${attempt}/${maxRetries}...`);
            await page.goto(url, { waitUntil: 'networkidle', timeout: 30000 });
            success = true;
        } catch (e) {
            console.warn(`   ⚠️ Attempt ${attempt} failed: ${e.message}`);
            if (attempt === maxRetries) {
                console.error(`❌ Failed to load page after ${maxRetries} attempts.`);
                process.exit(1);
            }
            // Wait before retrying (exponential backoff)
            const delay = Math.pow(2, attempt) * 1000;
            await new Promise(resolve => setTimeout(resolve, delay));
            attempt++;
        }
    }

    try {
        // Check for Fluent UI components or specific text
        const title = await page.title();
        console.log(`✅ Page Title: ${title}`);

        // Check for the presence of blazor script in the DOM (supporting fingerprints)
        const blazorScriptFound = await page.evaluate(() => {
            const scripts = Array.from(document.querySelectorAll('script'));
            return scripts.some(s => /_framework\/blazor\.(web|server|webassembly)(\..*)?\.js/.test(s.src));
        });

        if (blazorScriptFound) {
            console.log('✅ Blazor framework script found in DOM.');
        } else {
            console.warn('⚠️ Blazor framework script not found in DOM. This might be okay if it was bundled differently, but checking console errors is more important.');
        }

        console.log('✨ Production deployment looks healthy!');
    } catch (e) {
        console.error(`❌ Failed to load page: ${e.message}`);
        process.exit(1);
    } finally {
        await browser.close();
    }
}

verify();
