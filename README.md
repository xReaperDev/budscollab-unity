# BudsCollab for Unity

Unity Package Manager package for connecting the Unity editor to BudsCollab spaces and checking selected scene assets.

## Install from Git

Use Unity Package Manager:

1. Open `Window > Package Manager`.
2. Click `+`.
3. Choose `Add package from git URL...`.
4. Paste the package URL.

Use this Git URL:

```txt
https://github.com/xReaperDev/budscollab-unity.git
```

## Use

Open `Window > BudsCollab`.

1. Click `Open BudsCollab Login` if you need to sign in.
2. Create a read-only MCP token from the BudsCollab MCP connect guide, then paste it into `Access Token`.
3. Click `Connect and Load Spaces`.
4. Pick a fetched space and room from the dropdowns.
5. Use `Open Selected Room` to open that room in BudsCollab.
6. Select scene objects and run `Check Selected Objects`.

This package is intentionally Unity-only. Cross-app handoff, import, upload, and publish controls are not shown until those flows have real endpoints.
