using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.Json;
using Csv;
using TMPro;
using System.Text;
using System.IO;
using System;

public class TypingManager : MonoBehaviour
{
    private TextMeshPro sentence;
    private TextMeshPro hiragana;
    private TextMeshPro alp;
    private TextMeshPro typing_count;
    private TextMeshPro miss_typ_count;

    private List<string> Q_sentence = new List<string>();
    private List<string> Q_hiragana = new List<string>();

    private static readonly string current_directory = Environment.CurrentDirectory;
    private readonly string csv_path = current_directory + "/Assets/Scripts/workbook.csv";
    private readonly string json_path = @"Typing/Assets/Scripts/romanTypingParseDictionary.json";

    private (List<string>, List<string>) Read_Csv(string path)//ñ‚ëËèWÇÃì«Ç›çûÇ›
    {
        Dictionary<string, string> question_dic = new Dictionary<string, string>();
        string csv = File.ReadAllText(path);
        foreach (ICsvLine line in CsvReader.ReadFromText(csv))
        {
            question_dic.Add(line[0], line[1]);
        }
        return (new List<string>(question_dic.Keys), new List<string>(question_dic.Values));
    }
}
