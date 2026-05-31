using UnityEngine;
using UnityEditor;

public class TestMenu
{
    [MenuItem("Tools/SortGems/Test Simple Command")]
    public static void TestCommand()
    {
        Debug.Log("Test command executed successfully!");
        var go = new GameObject("TestObject");
        Undo.RegisterCreatedObjectUndo(go, "Create TestObject");
    }
}
