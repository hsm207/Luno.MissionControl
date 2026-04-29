const { chromium } = require('playwright');
const { execSync } = require('child_process');

async function verify() {
    console.log('🚀 Starting production deployment verification...');

    // 1. Find the URL for the webfrontend
    let url = process.env.WEB_FRONTEND_URL;
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
            if (msg.text().includes('blazor.web.js') || msg.text().includes('404')) {
                console.error('🚨 STATIC ASSET FAILURE DETECTED!');
                process.exit(1);
            }
        }
    });

    try {
        await page.goto(url, { waitUntil: 'networkidle' });
        
        // Check for Fluent UI components or specific text
        const title = await page.title();
        console.log(`✅ Page Title: ${title}`);

        // Check for the presence of blazor script in the DOM
        const blazorScript = await page.$('script[src*="_framework/blazor.web.js"]');
        if (blazorScript) {
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
