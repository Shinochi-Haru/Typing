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
        private readonly string csv_path = current_directory + "/Assets/script/workbook_sample_2.csv";
        private readonly string json_path = current_directory + @"/Assets/script/romanTypingParseDictionary.json";
        private int parse_index = 0;
        private int patten_num = 0;
        private int word_num = 0;
        private int miss;
        private int typ_count;
        private List<int> ramdom_list = new List<int>();
        private int Q_index = 0;
        private string alpha_script;
        private int alpha_index = 0;
        private List<int> patten_hound;
        private bool ramdom_switch = true;
        Animator anim,anim2;
        [SerializeField] private GameObject lazer; //���[�U�[�v���n�u���i�[
        [SerializeField] private Transform attackPoint;//�A�^�b�N�|�C���g���i�[
        [SerializeField] private float attackTime = 0.2f; //�U���̊Ԋu
        private float currentAttackTime; //�U���̊Ԋu���Ǘ�
        private bool canAttack; //�U���\��Ԃ����w�肷��t���O


        private (List<string>, List<string>) Read_Csv(string path)//���W�̓ǂݍ���
        { 
        Dictionary<string, string> question_dic = new();
        string csv = File.ReadAllText(path);
        foreach (ICsvLine line in CsvReader.ReadFromText(csv))
        {
            question_dic.Add(line[0], line[1]);
        }
        return (new List<string>(question_dic.Keys), new List<string>(question_dic.Values));
        }

        private static readonly Dictionary<string, string[]> mapping = new();
        private void Read_Json_File(string path)//�p�[�X���̓ǂݍ���
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
                    validTypeList = new List<string>(mapping[tri]);
                    idx += 3;
                    parsedStr.Add(tri);
                }
                else if (mapping.ContainsKey(bi))
                {
                    validTypeList = new List<string>(mapping[bi]);
                    idx += 2;
                    parsedStr.Add(bi);
                }
                else
                {
                    validTypeList = new List<string>(mapping[uni]);
                    // �����u��v�̏���
                    if (uni.Equals("��") && sentenceHiragana.Length - 1 == idx)
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
        //Typing_Manager�N���X�O�ɋL�q
        class RomanMapping
        {
            // �p�[�X�����Ƃ��̃p�^�[��
            public string Pattern { get; set; } = "";
            // �p�^�[���ɑ΂��郍�[�}�����͂̑ł���
            public string[] TypePattern { get; set; } = new string[1] { "" };
        }
        

        private void Start()
        {
        sentence = GameObject.Find("sentense-text").GetComponent<TextMeshPro>();
        hiragana = GameObject.Find("hiragana-text").GetComponent<TextMeshPro>();
        alp = GameObject.Find("alp-text").GetComponent<TextMeshPro>();
        typing_count = GameObject.Find("type-count").GetComponent<TextMeshPro>();
        miss_typ_count = GameObject.Find("miss-count").GetComponent<TextMeshPro>();

            (Q_sentence, Q_hiragana) = Read_Csv(csv_path);
            Read_Json_File(json_path);
            sentence.text = Q_sentence[0];
            hiragana.text = Q_hiragana[0];
            result = ConstructTypeSentence(Q_hiragana[0]);

            if (ramdom_switch == true)
        {
            Random_Question();
            sentence.text = Q_sentence[ramdom_list[Q_index]];
            hiragana.text = Q_hiragana[ramdom_list[Q_index]];
            result = ConstructTypeSentence(Q_hiragana[ramdom_list[Q_index]]);
        }
        else
        {
            sentence.text = Q_sentence[ramdom_list[Q_index]];
            hiragana.text = Q_hiragana[ramdom_list[Q_index]];
            result = ConstructTypeSentence(Q_hiragana[ramdom_list[Q_index]]);
        }
            Reset_Patten();
            Parse_Look(result);
            Parse_Mixed(result);
            anim = GetComponent<Animator>();
            anim2 = GetComponent<Animator>();
            currentAttackTime = attackTime;
        }

        private void Parse_Look((List<string>, List<List<string>>) result)//�p�[�X�m�F�p
        {
            for (int i = 0; i < result.Item1.Count; ++i)
            {
                var partStr = result.Item1[i];
                Debug.Log($"Part {i} : {partStr}");
                var strBuilder = new StringBuilder();
                for (int j = 0; j < result.Item2[i].Count; ++j)
                {
                    if (j == result.Item2[i].Count - 1)//�܂����ɓ��͕��@�����邩�A�Ȃ���
                    {
                        strBuilder.Append(result.Item2[i][j]);
                    }
                    else
                    {
                        strBuilder.Append($"{result.Item2[i][j]}, ");//�h,�h�ƂƂ��ɒǉ�
                    }
                }
                Debug.Log(strBuilder.ToString());
            }
        }

        private void Parse_Mixed((List<string>, List<List<string>>) result)//�p�[�X�����Ƀ��[�}�����̍쐬
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
            }
            alp.text = parse_total;
            alpha_script = parse_total;
        }
        private void OnGUI()
        {
        if (Input.anyKey)
        {
            string inkey = Input.inputString.ToString();

            if (inkey != null ^ inkey == "")//Shift�L�[�ȂǕ�����񂪊܂܂�Ȃ��L�[��r��
            {
                    switch (Input_Judge(inkey))
                    {
                        case 1:
                            alp.text = "<color=red>" + alpha_script.Insert(alpha_index + 1, "</color>");
                            alpha_index++; //�A���t�@�x�b�g�����ړ�
                            word_num++;    //�p�^�[�����̎��̕����Ɉړ�
                            if (word_num == result.Item2[parse_index][patten_num].Length) //���̕������Ȃ������玟�̃p�[�X�Ɉړ�
                            {
                                parse_index++;
                                patten_num = 0;
                                word_num = 0;
                            }
                                Question_Change();
                            break;
                        case 2://�^�C�v�~�X
                            miss++;
                            miss_typ_count.text = miss.ToString();
                            break;
                    }
                    typ_count++;
                    typing_count.text = typing_count.ToString();
            }
        }
            Input.ResetInputAxes();
    }
        public int Input_Judge(string inkey)//���͂̍���
        {
            if (result.Item2[parse_index][patten_num][word_num].ToString() == inkey)
            {
                return 1;
            }
            else
            {
                List<string> answer = result.Item2[parse_index].FindAll(answer => answer[patten_num].ToString() == inkey);
                if (answer.Count != 0)
                {
                    int num = 0;
                    foreach (string typePattern in result.Item2[parse_index])
                    {
                        if (typePattern == answer[0])//����������̂ň�ԒZ������
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
            }
        }

        public void Question_Change()  //���ύX
        {
            if (alpha_script.Length == alpha_index)
            {
                parse_index = 0;
                alpha_index = 0;
                Q_index++;
                if (ramdom_switch == true)
                {
                    sentence.text = Q_sentence[ramdom_list[Q_index]];
                    hiragana.text = Q_hiragana[ramdom_list[Q_index]];
                    result = ConstructTypeSentence(Q_hiragana[ramdom_list[Q_index]]);
                    anim.Play("hero_attack");
                    Attack();
                }
                else
                {
                    sentence.text = Q_sentence[Q_index];
                    hiragana.text = Q_hiragana[Q_index];
                    result = ConstructTypeSentence(Q_hiragana[Q_index]);
                    anim.Play("hero_attack");
                    Attack();
                }
                Reset_Patten();
                Parse_Mixed(result);
            }
        }

        private void Reset_Patten()//�p�[�X�p�^�[���̏�����
        {
            patten_hound = new List<int>();
            for (int i = 0; i < result.Item1.Count; i++)
            {
                patten_hound.Add(0);
            }
        }
      private void Random_Question()//���̃����_����
      {
        System.Random dice = new System.Random();
        for (int i = 0; i < Q_sentence.Count; i++)
        {
            while (true)
            {
                int num = dice.Next(0, Q_sentence.Count);
                if (i == 0)  //����͒��g�������̂Ŕ�r�ł��Ȃ�
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


        //void Update()
        //{
        //    if (ramdom_switch == true)
        //    {
        //        Attack();
        //    }
        //}



        void Attack()
        {
            //attackTime += Time.deltaTime; //attackTime�ɖ��t���[���̎��Ԃ����Z���Ă���

            //if (attackTime > currentAttackTime)
            //{
            //    canAttack = true; //�w�莞�Ԃ𒴂�����U���\�ɂ���
            //}

            //if (ramdom_switch == true) //K�L�[����������
            //{

            //    //�������ɐ�������I�u�W�F�N�g�A��������Vector3�^�̍��W�A��O�����ɉ�]�̏��
                Instantiate(lazer, attackPoint.position, Quaternion.identity);
                
            //}
        }
    }
}