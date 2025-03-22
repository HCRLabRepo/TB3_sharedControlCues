using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;


public class Data2CSV : MonoBehaviour
{
    public string name;
    public int index;

    // public List<Data> records = new List<Data>();

    public void ExportToCsv()
    {
        var records = DataManager.Instance.Records;
        
        StringBuilder sb = new StringBuilder();

        // 添加 CSV 表头
        sb.AppendLine("ID,Value");

        // 添加数据行
        foreach (var record in records)
        {
            sb.AppendLine($"{record.DataName},{record.Value}");
        }

        // 文件保存路径
        string filePath = $"C:\\Users\\ourou\\Downloads\\data_{name}_{index}.csv";

        // 写入文件
        File.WriteAllText(filePath, sb.ToString());

        Debug.Log("CSV 文件已导出至: " + filePath);
    }
}
