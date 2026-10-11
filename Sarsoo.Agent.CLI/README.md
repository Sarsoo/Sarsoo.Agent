# reeve

A TUI agent harness to demonstrate [Sarsoo.Agent](../Sarsoo.Agent).

# Config

`~/.config/reeve/config.json`

```jsonc
{
  "Provider": {
    "Url": "http://ai:8080/v1",
    "Key": "sk-oiajdoaisjd",
    "Model": "gpt-3.5-turbo"
  },
  "Agents": {
    "DefaultProfile": "coding",
    "Startup": [
      {
        "Name": "default",
        "Profile": "coding",
        "Model": "", // optional
        "ThinkingLevel": "", // optional: None|Low|Medium|High|ExtraHigh
        "Prompt": "Do a good job" // optional
      }
    ]
  },
  "Advisor": {
    "Enabled": true,
    "Model": "gpt-3.5-turbo", // optional
    "ThinkingLevel": "", // optional
    "SystemPrompt": "", // optional
    "ToolName": "", // optional
    "ToolDescription": "" // optional
  },
  "ToolGate": {
    "DefaultResult": true,
    "PolicyRule": "Any"
  },
  "Stubbed": false
}
```