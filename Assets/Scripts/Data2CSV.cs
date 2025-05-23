using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using TMPro;


public class Data2CSV : MonoBehaviour
{
    public string name;
    public int index;
    //public TMP_InputField user_id;
    //public TMP_InputField trial_index;

    // public List<Data> records = new List<Data>();
    

    public void ExportToCsv()
    {
        
        var records = DataManager.Instance.Records;
        
        
        StringBuilder sb = new StringBuilder();

        // add header
        sb.AppendLine("ID,Value");

        // add data
        foreach (var record in records)
        {
            sb.AppendLine($"{record.DataName},{record.Value}");
        }

        // save file
        string filePath = $"C:\\Users\\ourou\\Downloads\\{name}_{DataManager.Instance.modeName}_{index}.csv";

        // write to file
        File.WriteAllText(filePath, sb.ToString());

        Debug.Log("CSV 文件已导出至: " + filePath);
    }
}
