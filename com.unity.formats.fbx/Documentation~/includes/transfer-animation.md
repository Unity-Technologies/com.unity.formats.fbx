Optional. Transfer the animation from a GameObject to one of its descendants being in the exported GameObject hierarchy.

For example, when you export a character with mesh and skeleton held by separate child GameObjects, you might need to transfer the animation of the character to its skeleton to avoid mesh/skeleton separation at import in another modeling application.

| Property | Function |
| :--- | :--- |
| **Source** | The GameObject to transfer the animation from.<br /><br />• This GameObject must be an ancestor of the **Destination**.<br />• This GameObject can be an ancestor of the GameObject to export. |
| **Destination** | The GameObject to transfer the animation to.<br /><br />• This GameObject receives the animation from the **Source** as well as from any objects in between in the hierarchy. |
