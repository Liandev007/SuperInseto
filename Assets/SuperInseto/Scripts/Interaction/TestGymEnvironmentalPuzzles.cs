using UnityEngine;

namespace SuperInseto
{
    [DefaultExecutionOrder(-210)]
    public sealed class TestGymEnvironmentalPuzzles : MonoBehaviour
    {
        [SerializeField] DestructibleObject panelPrefab;
        [SerializeField] BioelectricReceiver receiverPrefab;
        [SerializeField] Transform panelPoint;
        [SerializeField] Transform nodeA;
        [SerializeField] Transform nodeB;
        [SerializeField] GameObject barrier;
        [SerializeField] SlidingDoor door;
        [SerializeField] Transform player;
        [SerializeField] Vector3 feedbackCenter = new Vector3(-17f, 2f, 5f);
        [SerializeField] Vector3 feedbackSize = new Vector3(10f, 6f, 14f);
        [SerializeField] string panelSaveId = "m12_sabotage_panel";
        [SerializeField] string nodeASaveId = "m12_node_a";
        [SerializeField] string nodeBSaveId = "m12_node_b";
        EnvironmentalPuzzle sabotage, circuit;
        void Awake()
        {
            if (!panelPrefab || !receiverPrefab || !panelPoint || !nodeA || !nodeB || !barrier || !door)
            { Debug.LogError("M12 gym has missing prefab, point or output references.", this); return; }
            var panel = Instantiate(panelPrefab, panelPoint.position, panelPoint.rotation, transform);
            panel.name = "M12 - painel de sabotagem - 80 HP";
            var a = Instantiate(receiverPrefab, nodeA.position, nodeA.rotation, transform); a.name = "Bioelectric Node A";
            var b = Instantiate(receiverPrefab, nodeB.position, nodeB.rotation, transform); b.name = "Bioelectric Node B";
            PersistentId.Assign(panel.gameObject, panelSaveId);
            PersistentId.Assign(a.gameObject, nodeASaveId);
            PersistentId.Assign(b.gameObject, nodeBSaveId);
            sabotage = NewPuzzle("Puzzle A - destruir painel");
            sabotage.Configure(new[] { panel }, null, () => { if (barrier) barrier.SetActive(false); });
            sabotage.gameObject.SetActive(true);
            circuit = NewPuzzle("Puzzle B - energizar A e B");
            circuit.Configure(null, new[] { a, b }, () => { if (door) door.OpenFromControl(); });
            circuit.gameObject.SetActive(true);
        }
        EnvironmentalPuzzle NewPuzzle(string label)
        {
            var go = new GameObject(label); go.SetActive(false); go.transform.SetParent(transform, false);
            return go.AddComponent<EnvironmentalPuzzle>();
        }
        void OnGUI()
        {
            if (!player || !sabotage || !circuit || !new Bounds(feedbackCenter, feedbackSize).Contains(player.position)) return;
            string a = sabotage.IsSolved ? "A: SISTEMA DESATIVADO — passagem livre" : "A: destrua o painel de controle para desligar a barreira";
            string b = circuit.IsSolved ? "B: CIRCUITO ENERGIZADO — porta aberta" : "B: use F nos circuitos — " + circuit.ConditionsMet + "/2 ligados";
            GUI.Box(new Rect(12f, 12f, 450f, 52f), a + "\n" + b);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var point in new[] { panelPoint, nodeA, nodeB })
                if (point) Gizmos.DrawWireSphere(point.position + Vector3.up * 1.2f, 0.5f);
        }
    }
}
