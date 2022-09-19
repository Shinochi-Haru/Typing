using System.Collections.Generic;
using UnityEngine;
using Csv;
using TMPro;
using System.Text;
using System.IO;
using System;
using System.Text.Json;

namespace RomanTypingParser
{

public class TypingManager : MonoBehaviour
{
    private TextMeshPro sentence;
    private TextMeshPro hiragana;
    private TextMeshPro alp;
    private TextMeshPro typing_count;
    private TextMeshPro miss_typ_count;

    private List<string> Q_sentence = new List<string>();
    private List<string> Q_hiragana = new List<string>();
    private (List<string> parsedSentence, List<List<string>> judgeAutomaton) result;
    private static readonly string current_directory = Environment.CurrentDirectory;
    private readonly string csv_path = current_directory + "/Assets/script/workbook_sample_1.csv";
    private readonly string json_path = "Typing/Assets/script/romanTypingParseDictionary.json";

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
        private bool ramdom_switch = true;

        private void Start()
    {
        sentence = GameObject.Find("sentense-text").GetComponent<TextMeshPro>();
        hiragana = GameObject.Find("hiragana-text").GetComponent<TextMeshPro>();
        alp = GameObject.Find("alp-text").GetComponent<TextMeshPro>();
        typing_count = GameObject.Find("type-count").GetComponent<TextMeshPro>();
        miss_typ_count = GameObject.Find("miss-count").GetComponent<TextMeshPro>();

        //(Q_sentence, Q_hiragana) = Read_Csv(csv_path);
        //Read_Json_File(json_path);
        //sentence.text = Q_sentence[0];
        //hiragana.text = Q_hiragana[0];
        //result = ConstructTypeSentence(Q_hiragana[0]);


        (Q_sentence, Q_hiragana) = Read_Csv(csv_path);
        Read_Json_File(json_path);
        if (ramdom_switch == true)
        {
            Random_Question();
            sentence.text = Q_sentence[ramdom_list[0]];
            hiragana.text = Q_hiragana[ramdom_list[0]];
            result = ConstructTypeSentence(Q_hiragana[ramdom_list[0]]);
        }
        else
        {
            sentence.text = Q_sentence[0];
            hiragana.text = Q_hiragana[0];
            result = ConstructTypeSentence(Q_hiragana[0]);
        }
        Reset_Patten();
        Parse_Look(result);
        Parse_Mixed(result);
    }

    private void Parse_Look((List<string>, List<List<string>>) result)//パース確認用
    {
        for (int i = 0; i < result.Item1.Count; ++i)
        {
            var partStr = result.Item1[i];
            Debug.Log($"Part {i} : {partStr}");
            var strBuilder = new StringBuilder();
            for (int j = 0; j < result.Item2[i].Count; ++j)
            {
                if (j == result.Item2[i].Count - 1)//まだ他に入力方法があるか、ないか
                {
                    strBuilder.Append(result.Item2[i][j]);
                }
                else
                {
                    strBuilder.Append($"{result.Item2[i][j]}, ");//”,”とともに追加
                }
            }
            Debug.Log(strBuilder.ToString());
        }
    }

    private void Parse_Mixed((List<string>, List<List<string>>) result)//パースを元にローマ字文の作成
    {
        string parse_total = "";

        for (int i = 0; i < result.Item1.Count; i++)
        {
            if (patten_hound[i] != 0)
            {
                parse_total += result.Item2[i][patten_num];
            }
            else
            {
                parse_total += result.Item2[i][0];
            }
            alp.text = parse_total;
            alpha_script = parse_total;
        }

    }
    private void OnGUI()
    {
        if (Input.anyKey)
        {
            string inkey = Input.inputString.ToString();

            if (inkey != null ^ inkey == "")//Shiftキーなど文字情報が含まれないキーを排他
            {
                // if(inkey != null ^ inkey == "") の中

                switch (Input_Judge(inkey))
                {
                    case 1:
                        alp.text = "<color=red>" + alpha_script.Insert(alpha_index + 1, "</color>");
                        alpha_index++; //アルファベット文字移動
                        word_num++;    //パターン内の次の文字に移動
                        if (word_num == result.Item2[parse_index][patten_num].Length) //次の文字がなかったら次のパースに移動
                        {
                            parse_index++;
                            patten_num = 0;
                            word_num = 0;
                        }
                        Question_Change();
                        break;
                    case 2://タイプミス
                        break;
                }
            }
        }
    }
    private int parse_index = 0;
    private int patten_num = 0;
    private int word_num = 0;
    private int miss;
    private int typ_count;
    private List<int> ramdom_list = new List<int>();

    private int Input_Judge(string inkey)//入力の合否
    {
        List<string> answer = result.Item2[parse_index].FindAll(answer => answer[word_num].ToString() == inkey);
        //if (result.Item2[parse_index][patten_num][word_num].ToString() == inkey)
        //{
        //    return 1;
        //}
        //else
        //{
        //    return 2;
        //}
        if (answer.Count != 0)
        {//柔軟入力に対応していた場合
            int num = 0;
            foreach (string typePattern in result.Item2[parse_index])
            {
                if (typePattern == answer[0])//符合するもので一番短いもの
                {
                    patten_num = num;
                    patten_hound[parse_index] = patten_num;
                    Parse_Mixed(result);
                    break;
                }
                num++;
            }

            return 1;
        }
        else
        {
            return 2;
        }
        switch (Input_Judge(inkey))
        {
            case 1:


                break;
            case 2:
                miss++;
                miss_typ_count.text = miss.ToString();
                break;
        }
        Input.ResetInputAxes();
        typ_count++;
        typing_count.text = typ_count.ToString();
    }

    private string alpha_script;
    private int alpha_index = 0;
    private List<int> patten_hound;

    private void Question_Change()  //問題変更
    {
        if (alpha_script.Length == alpha_index)
        {
            parse_index = 0;
            alpha_index = 0;
            List<int> question_index = new List<int>();
            question_index[0]++;
            if (ramdom_switch == true)
            {
                sentence.text = Q_sentence[ramdom_list[question_index[0]]];
                hiragana.text = Q_hiragana[ramdom_list[question_index[0]]];
                result = ConstructTypeSentence(Q_hiragana[ramdom_list[question_index[0]]]);
            }
            else
            {
                sentence.text = Q_sentence[question_index[0]];
                hiragana.text = Q_hiragana[question_index[0]];
                result = ConstructTypeSentence(Q_hiragana[question_index[0]]);
            }
            Reset_Patten();
            Parse_Mixed(result);
        }
    }

    private void Reset_Patten()//パースパターンの初期化
    {
        patten_hound = new List<int>();
        for (int i = 0; i < result.Item1.Count; i++)
        {
            patten_hound.Add(0);
        }
    }
    private void Random_Question()//問題のランダム化
    {
        System.Random dice = new System.Random();
        for (int i = 0; i < Q_sentence.Count; i++)
        {
            while (true)
            {
                int num = dice.Next(0, Q_sentence.Count);
                if (i == 0)  //初回は中身が無いので比較できない
                {
                    ramdom_list.Add(num);
                    break;
                }
                else if (ramdom_list.Contains(num) == false)
                {
                    ramdom_list.Add(num);
                    break;
                }
                else
                {
                    continue;
                }
            }
        }
    }
}
}