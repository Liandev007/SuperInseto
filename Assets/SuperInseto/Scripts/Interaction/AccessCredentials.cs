using System.Collections.Generic;
using UnityEngine;

namespace SuperInseto
{
    public sealed class AccessCredentials : MonoBehaviour
    {
        readonly HashSet<string> granted = new HashSet<string>();
        public bool Has(string id) => !string.IsNullOrWhiteSpace(id) && granted.Contains(id);
        public bool Grant(string id) => !string.IsNullOrWhiteSpace(id) && granted.Add(id);
    }
}
