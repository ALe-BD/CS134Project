using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System;
using UnityEngine;

public class DebugVariableDumper : MonoBehaviour
{
    [SerializeField] private MonoBehaviour targetScript;
    [SerializeField] private Component targetComponent;
    [TextArea(10, 30)] public string dumpedVariables;
    [TextArea(10, 30)] public string dumpedComponentVariables;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        DumpVariables();
        DumpComponentVariables();
    }
    public void DumpVariables()
    {
        if (targetScript == null)
        {
            dumpedVariables = "No target script assigned.";
            return;
        }

        StringBuilder sb = new StringBuilder();

        System.Type type = targetScript.GetType();
        sb.AppendLine("Script: " + type.Name);

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(targetScript);
            sb.AppendLine(field.Name + " = " + (value != null ? value.ToString() : "null"));
        }

        dumpedVariables = sb.ToString();
    }
        [ContextMenu("Read Component")]
    public void DumpComponentVariables()
    {
        if (targetComponent == null)
        {
            dumpedComponentVariables = "No component assigned.";
            Debug.LogWarning(dumpedComponentVariables);
            return;
        }

        StringBuilder sb = new StringBuilder();
        Type type = targetComponent.GetType();

        sb.AppendLine("Component: " + type.Name);
        sb.AppendLine("--- Fields ---");

        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(targetComponent);
            sb.AppendLine(field.Name + " = " + FormatValue(value));
        }

        sb.AppendLine();
        sb.AppendLine("--- Properties ---");

        PropertyInfo[] properties = type.GetProperties(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (PropertyInfo prop in properties)
        {
            if (!prop.CanRead) continue;
            if (prop.GetIndexParameters().Length > 0) continue;

            try
            {
                object value = prop.GetValue(targetComponent, null);
                sb.AppendLine(prop.Name + " = " + FormatValue(value));
            }
            catch
            {
                sb.AppendLine(prop.Name + " = <unavailable>");
            }
        }

        dumpedComponentVariables = sb.ToString();
    }

    private string FormatValue(object value)
    {
        if (value == null)
            return "null";

        if (value is Vector2 v2)
            return $"({v2.x}, {v2.y})";

        if (value is Vector3 v3)
            return $"({v3.x}, {v3.y}, {v3.z})";

        if (value is Vector4 v4)
            return $"({v4.x}, {v4.y}, {v4.z}, {v4.w})";

        if (value is Quaternion q)
            return $"({q.x}, {q.y}, {q.z}, {q.w})";

        return value.ToString();
    }
}
