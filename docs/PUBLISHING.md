# 上传 GitHub

准备两个独立附件：源码 ZIP 用于仓库，win-x64 ZIP 用于 Releases。不要上传私人开发目录，也不要上传 out/build/vendor/runtime/evidence 目录。源码包已经按允许清单清理；.gitignore 是额外保护，不应替代打包清单。

建议仓库名称 `550c-crt-launcher`；介绍：Independent gray CRT boot launcher and appearance preset for the official ChatGPT Desktop on Windows. 项目图标使用原创终端符号，不把 OpenAI 标志当项目品牌。

先查看 README、LICENSE、CREDITS、THIRD_PARTY_NOTICES、docs/VALIDATION.md。源码可在 GitHub 新仓库的 Add file → Upload files 上传解压后的文件；为保留 `.github`、`.gitignore` 等隐藏项目，通常更适合使用 Git 客户端。先检查将提交的文件清单，不执行全私人项目的 `git add .`。

新建仓库后，可在干净源码目录手动执行：

```text
git init
git add README.md LICENSE CREDITS.md THIRD_PARTY_NOTICES.md CHANGELOG.md SECURITY.md CONTRIBUTING.md AGENTS.md .gitignore .gitattributes src boot config themes assets scripts docs tests third-party .github
git diff --cached --stat
git commit -m "Prepare 550C CRT launcher preview"
git branch -M main
git remote add origin <你实际创建的仓库地址>
git push -u origin main
```

版本建议 `v0.1.0-preview.1`，创建 Release 时勾选 **Pre-release**。附件放 win-x64 ZIP 与 SHA256SUMS.txt；不要用“稳定版”掩盖未验收的真实冷启动、前台交接、字体导入和多设备条件。CI 只构建与核验包，未经验证的远程运行测试不会被声称通过。

这份说明没有替你创建远程仓库、推送代码或发布 Release。上传/仓库可见性/作者身份由仓库主人决定。MIT 用于此公开版的独立项目实现；第三方组件及标志权利不转让。
