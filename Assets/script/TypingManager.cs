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

    private (List<string>, List<string>) Read_Csv(string path)//問題集の読み込み
    {
        Dictionary<string, string> question_dic = new Dictionary<string, string>();
        string csv = File.ReadAllText(path);
        foreach (ICsvLine line in CsvReader.ReadFromText(csv))
        {
            question_dic.Add(line[0], line[1]);
        }
        return (new List<string>(question_dic.Keys), new List<string>(question_dic.Values));
    }

    private static readonly Dictionary<string, string[]> mappingDict = new();

    private void Read_Json_File(string path)//パース情報の読み込み
    {
        if (mapping.Count != 0) { return; }
        var reader = new StreamReader(path, Encoding.GetEncoding("utf-8"));
        var jsonStr = reader.ReadToEnd();
        reader.Close();

        var data = JsonSerializer.Deserialize<RomanMapping[]>(jsonStr);
        if (data != null)
        {
            foreach (var mapData in data)
            {
                mapping.Add(mapData.Pattern, mapData.TypePattern);
            }
        }
        return;
    }

    public static (List<string> parsedSentence, List<List<string>> judgeAutomaton) ConstructTypeSentence(string sentenceHiragana)
    {
        int idx = 0;
        string uni, bi, tri;
        var judge = new List<List<string>>();
        var parsedStr = new List<string>();
        while (idx < sentenceHiragana.Length)
        {
            List<string> validTypeList;
            uni = sentenceHiragana[idx].ToString();
            bi = (idx + 1 < sentenceHiragana.Length) ? sentenceHiragana.Substring(idx, 2) : "";
            tri = (idx + 2 < sentenceHiragana.Length) ? sentenceHiragana.Substring(idx, 3) : "";
            if (mapping.ContainsKey(tri))
            {
                validTypeList = new List<string>(mappingDict[tri]);
                idx += 3;
                parsedStr.Add(tri);
            }
            else if (mapping.ContainsKey(bi))
            {
                validTypeList = new List<string>(mappingDict[bi]);
                idx += 2;
                parsedStr.Add(bi);
            }
            else
            {
                validTypeList = new List<string>(mappingDict[uni]);
                // 文末「ん」の処理
                if (uni.Equals("ん") && sentenceHiragana.Length - 1 == idx)
                {
                    validTypeList.Remove("n");
                }
                idx++;
                parsedStr.Add(uni);
            }
            judge.Add(validTypeList);
        }
        return (parsedStr, judge);
    }

    //Typing_Managerクラス外に記述
    class RomanMapping
    {
        // パースされるときのパターン
        public string Pattern { get; set; } = "";
        // パターンに対するローマ字入力の打ち方
        public string[] TypePattern { get; set; } = new string[1] { "" };
    }

    private void Start()
    {
        sentence = GameObject.Find("sentence_text").GetComponent<TextMeshPro>();
        hiragana = GameObject.Find("hiragana_text").GetComponent<TextMeshPro>();
        alp = GameObject.Find("alpha_text").GetComponent<TextMeshPro>();
        typing_count = GameObject.Find("type_count").GetComponent<TextMeshPro>();
        miss_typ_count = GameObject.Find("miss_count").GetComponent<TextMeshPro>();
    }
}
