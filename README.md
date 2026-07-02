# BudsCollab for Unity

Unity Package Manager package for testing BudsCollab asset workflows inside the
Unity editor.

## Install from Git

Use Unity Package Manager:

1. Open `Window > Package Manager`.
2. Click `+`.
3. Choose `Add package from git URL...`.
4. Paste the package URL.

When this package is exported to its public repo, use:

```txt
https://github.com/xReaperDev/budscollab-unity.git
```

For private monorepo testing, use:

```txt
https://github.com/xReaperDev/budscollab.git?path=/integrations/unity#reaperdev/bc-275-research-and-unify-budssdk-budsapp-budscollab-primitives
```

## Use

Open `Window > BudsCollab`.

The first test shell includes login, space/room/wall selection, asset browser
placeholder, validation, upload selected, open web preview, and publish controls.
The public product name is BudsCollab for Unity; internally it still uses the
BudsCollab bridge protocol and `@budscollab/buds-bridge-sdk` contracts.
