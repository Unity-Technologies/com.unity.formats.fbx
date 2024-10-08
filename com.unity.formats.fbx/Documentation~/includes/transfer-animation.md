Optionally transfer the animation from a GameObject to one of its descendants to export.

For example, when you export a character with mesh and skeleton held by separate child GameObjects, you might want to transfer the animation of the character to its skeleton to avoid mesh/skeleton separation issues when using in another modeling application.

| Property | Function |
| :--- | :--- |
| **Source** | The GameObject to transfer the animation from.<br /><br />• This GameObject must be an ancestor of the **Destination**.<br />• This GameObject doesn't have to be part of the export, it can be an ancestor of the GameObject to export. |
| **Destination** | The GameObject to transfer the animation to.<br /><br />• This GameObject receives the animation from the **Source** as well as from any GameObjects in between in the hierarchy. |
