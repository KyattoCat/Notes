需要魔法

irm https://claude.ai/install.ps1 | iex

也可以先把脚本弄到本地执行

安装完后不急着开

C:\Users\User\claude.json 添加

```json
{  
  "hasCompletedOnboarding": true
}
```

C:\Users\User\\.claude\\.settings.json 配置环境

```json
{
  "env": {
    "ANTHROPIC_AUTH_TOKEN": "",
    "ANTHROPIC_BASE_URL": "",
    "API_TIMEOUT_MS": "",
    "CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC": "1",
    "ANTHROPIC_DEFAULT_OPUS_MODEL": "",
    "ANTHROPIC_DEFAULT_SONNET_MODEL": "",
    "ANTHROPIC_DEFAULT_HAIKU_MODEL": ""
  },
  
  "skipWebFetchPreflight": true // Claude Code Fetch 会先往 https://claude.ai/api/web/domain_info?domain= 做验证，但是似乎被墙掉了，用这个跳过
}
```

