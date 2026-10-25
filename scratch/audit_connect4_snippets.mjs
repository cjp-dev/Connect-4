import fs from 'fs';
import path from 'path';

const WORKSPACE_DIR = 'C:\\Udvikling\\Spil\\Connect-4';
const DOCS_BRAIN_DIR = path.join(WORKSPACE_DIR, 'docs', 'brain');
const GUI_SPEC_FILE = path.join(WORKSPACE_DIR, 'Plans', 'general-game-gui-specification.md');

const markdownFiles = [
    ...fs.readdirSync(DOCS_BRAIN_DIR)
        .filter(f => f.endsWith('.md'))
        .map(f => path.join(DOCS_BRAIN_DIR, f)),
    GUI_SPEC_FILE
];

console.log(`================================================================`);
console.log(`Connect-4 Documentation & Snippet Linter`);
console.log(`Scanning ${markdownFiles.length} markdown documents...`);
console.log(`================================================================\n`);

const codeBlocks = [];
const discrepancies = [];

const GITHUB_REPO_PREFIX = 'https://github.com/cjp-dev/Connect-4/blob/main/';

markdownFiles.forEach(filePath => {
    const relPath = path.relative(WORKSPACE_DIR, filePath);
    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');

    let inBlock = false;
    let blockLang = '';
    let blockStart = 0;
    let blockContent = [];

    lines.forEach((line, idx) => {
        const lineNum = idx + 1;

        // Fenced code block detection
        const match = line.match(/^```(\w*)/);
        if (match) {
            if (!inBlock) {
                inBlock = true;
                blockLang = match[1] || 'text';
                blockStart = lineNum;
                blockContent = [];
            } else {
                inBlock = false;
                codeBlocks.push({
                    file: relPath,
                    lang: blockLang,
                    startLine: blockStart,
                    endLine: lineNum,
                    linesCount: lineNum - blockStart + 1,
                    content: blockContent.join('\n')
                });
            }
        } else if (inBlock) {
            blockContent.push(line);
        }

        // Link verification: check markdown links [text](target)
        const linkRegex = /\[([^\]]+)\]\(([^)]+)\)/g;
        let linkMatch;
        while ((linkMatch = linkRegex.exec(line)) !== null) {
            const linkText = linkMatch[1];
            const target = linkMatch[2].split('#')[0]; // strip anchor

            if (target.startsWith(GITHUB_REPO_PREFIX)) {
                // Verify that the target path exists in the repository
                const repoRelPath = decodeURIComponent(target.slice(GITHUB_REPO_PREFIX.length));
                const localResolved = path.join(WORKSPACE_DIR, repoRelPath);
                if (!fs.existsSync(localResolved)) {
                    discrepancies.push({
                        file: relPath,
                        line: lineNum,
                        type: 'BROKEN_GITHUB_LINK',
                        message: `GitHub link points to nonexistent file: '${repoRelPath}' (resolved to '${localResolved}').`
                    });
                }
            } else if (target.startsWith('file:///')) {
                const localPath = decodeURIComponent(target.replace(/^file:\/\/\//, ''));
                if (!fs.existsSync(localPath)) {
                    discrepancies.push({
                        file: relPath,
                        line: lineNum,
                        type: 'BROKEN_FILE_LINK',
                        message: `File URI '${target}' points to nonexistent path '${localPath}'.`
                    });
                }
            } else if (!target.startsWith('http://') && !target.startsWith('https://') && !target.startsWith('mailto:') && target.length > 0) {
                // Relative link
                const decodedTarget = decodeURIComponent(target);
                const fileDir = path.dirname(filePath);
                const resolvedTarget = path.resolve(fileDir, decodedTarget);

                if (!fs.existsSync(resolvedTarget)) {
                    discrepancies.push({
                        file: relPath,
                        line: lineNum,
                        type: 'BROKEN_RELATIVE_LINK',
                        message: `Broken relative link '${target}' -> resolved to nonexistent '${resolvedTarget}'.`
                    });
                }
            }
        }

        // Check for deprecated property bindings/mentions
        if (line.includes('Analysis.Value')) {
            discrepancies.push({
                file: relPath,
                line: lineNum,
                type: 'DEPRECATED_PROPERTY',
                message: `'Analysis.Value' is deprecated; use 'Analysis.Score'.`
            });
        }

        // Check for unsupported Mermaid syntax
        if (inBlock && blockLang === 'mermaid' && line.trim().startsWith('gantt')) {
            discrepancies.push({
                file: relPath,
                line: lineNum,
                type: 'UNSUPPORTED_MERMAID_TYPE',
                message: `'gantt' diagram type is unsupported in strict renderers; convert to 'flowchart LR'.`
            });
        }
    });

    // File-level checks
    if (relPath.includes('01-overview.md')) {
        if (!content.includes('173') || !content.includes('53') || !content.includes('226')) {
            discrepancies.push({
                file: relPath,
                line: 1,
                type: 'MISSING_TEST_BASELINE',
                message: `Chapter 01 does not document the test baseline: 173 Engine + 53 App = 226 passing tests.`
            });
        }
    }
});

// Summary reporting
console.log(`Found ${codeBlocks.length} fenced code blocks across documents:`);
console.log(`----------------------------------------------------------------`);
codeBlocks.forEach((b, i) => {
    console.log(`[${(i + 1).toString().padStart(2, ' ')}] ${b.file} L${b.startLine}-L${b.endLine} (${b.lang}, ${b.linesCount} lines)`);
});

console.log(`\n================================================================`);
console.log(`Discrepancy & Linter Report (${discrepancies.length} finding(s)):`);
console.log(`================================================================`);

if (discrepancies.length === 0) {
    console.log(`SUCCESS: 0 discrepancies detected. All documents in 100% compliance!`);
    process.exit(0);
} else {
    discrepancies.forEach((d, idx) => {
        console.log(`\n[Finding ${idx + 1}] ${d.type}`);
        console.log(`  File: ${d.file}:${d.line}`);
        console.log(`  Detail: ${d.message}`);
    });
    console.log(`\nLinter finished with ${discrepancies.length} active discrepancies to resolve.`);
    process.exit(1);
}
