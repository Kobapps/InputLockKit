using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Decorate a serialized <see cref="string"/> field (or a <c>List&lt;string&gt;</c>) to draw it as a
    /// tag field: a dropdown of the tags in the project's <see cref="InputLockTagCatalog"/>, with options
    /// to add a new tag (added to the catalog) or open the catalog.
    /// </summary>
    /// <example><code>[InputLockTag] public string lockTag;</code></example>
    public sealed class InputLockTagAttribute : PropertyAttribute
    {
    }
}
