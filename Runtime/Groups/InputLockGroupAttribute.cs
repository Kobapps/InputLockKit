using UnityEngine;

namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Decorate a serialized <see cref="string"/> field (or a <c>List&lt;string&gt;</c>) to draw it as a
    /// group field: a dropdown of the reusable groups in the project's <see cref="InputLockTagCatalog"/>,
    /// with options to add a new group (added to the catalog) or open the catalog.
    /// </summary>
    /// <example><code>[InputLockGroup] public string group;</code></example>
    public sealed class InputLockGroupAttribute : PropertyAttribute
    {
    }
}
