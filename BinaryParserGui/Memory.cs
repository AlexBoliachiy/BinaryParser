﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace BinaryParserGui
{
    public enum TYPE
    {
        cons,
        vars,
        arrs
    }

    public class Memory
    {
        private int m = -1;
        public string output = String.Empty;
        public string output_var = String.Empty;
        public string output_con = String.Empty;
        public string output_arr = String.Empty;

        private Dictionary<string, int >variables_values = new Dictionary<string, int>(); // Имя переменной : значение
        private Dictionary<string, TYPE> variable_type = new Dictionary<string, TYPE>(); // Имя переменной : тип.
        private Dictionary<string, int> variable_timeAdded = new Dictionary<string, int>(); // Имя перемеенной : какой по счету записанна
        private PostfixNotationExpression p = new PostfixNotationExpression();
        private Regex dec_con = new Regex(@"^const\s+[A-Za-z_]+[A-Z_a-z0-9]*\s*=\s*((((\d+)|([A-Za-z_]+[A-Z_a-z0-9]*)))|(b'[0-1]+)|(h'[0-9A-F]+))\s*$");
        private Regex dec_arr = new Regex(@"^Array\s+[A-Za-z_]+[A-Z_a-z0-9]*\s*\[\s*((([A-za-z_]+[A-Z_a-z0-9]*)|(\d+)))\s*(\s*[+\-*/]\s*((([A-za-z_]+[A-Z_a-z0-9]*)|(\d+))))*\s*:\s*0\]\s*=\s*\(\s*((([A-za-z_]+[A-Z_a-z0-9]*)|(\d+)))\s*(\s*,\s*((([A-za-z_]+[A-Z_a-z0-9]*)|(\d+))))*\s*\)\s*$");
        private Regex dec_var = new Regex(@"^[A-Za-z_]+[A-Z_a-z0-9]*\s*=\s*((\d+)|(h'[0-F]+)|(b'[10]+))\s*$");
        private Regex var = new Regex(@"[A-Za-z_]+[A-Z_a-z0-9]*"); 
        private Regex ca = new Regex(@"CA_[0-3]");
        private Regex expression = new Regex(@"(((([A-za-z_]+[A-Z_a-z0-9]*)|(\d+)))\s*(\s*[+\-*/]\s*((([A-za-z_]+[A-Z_a-z0-9]*)|(\d+))))+)|(h'[0-F]+)|(b'[01]+)");
        private int line;
        private int Cons = 0;
        private int Arrs = 0;
        private int Vars = 0;

        public Memory()
        {

        }

        public void Gather()
        {
            output = output_con + output_var + output_arr;  
        }



        

        public bool HandleDataString(string cmd)
        {
            cmd.Trim(); //Remove extra whitespaces
            string[] cmd_split = cmd.Split(' ');
            if (cmd_split[0] == "const")
            {



                if (dec_con.IsMatch(cmd))
                {
                    //Добавляем переменную и её значение в variables
                    //Добавляем в output данные
                    string const_name = cmd_split[1];
                    if (variable_type.ContainsKey(const_name))
                        return false;

                    int const_value = -1;
                    try {

                        if (char.IsDigit(cmd_split[3][0]))
                            const_value = Convert.ToInt32(ReplaceVariableToValue(cmd_split[3]));
                        else if (cmd_split[3].Remove(2) == "b'") // Binary number
                            const_value = Convert.ToInt32(cmd_split[3].Substring(2), 2);
                        else if (cmd_split[3].Remove(2) == "h'") //Hexadecimal number
                            const_value = Convert.ToInt32(cmd_split[3].Substring(2), 16);
                        else if (var.IsMatch(cmd_split[3]))
                            const_value = Convert.ToInt32(ReplaceVariableToValue(cmd_split[3]));
                        else
                            throw new CompilationException("Число");
                    }
                    catch 
                    {
                        throw new CompilationException("Допишіть будь ласка значення змінної у команді " + cmd);
                    }

                    if (const_name != "m")
                    {
                        if (Math.Pow(2, (double)m) - 1 < const_value)
                        {
                            throw new CompilationException("Переповнення стеку у команді " + cmd);
                        }
                        else
                        {
                            variables_values.Add(const_name, const_value);
                            variable_type.Add(const_name, TYPE.cons);
                            variable_timeAdded.Add(const_name, Cons++);
                            AddToOutput(const_value, ref output_con);

                            line++;
                        }
                    }
                    else
                    {
                        if (m == -1)
                            m = const_value;
                        else
                            throw new CompilationException("Повторне об'явлення m");
                    }


                }
                else return false;



            }
            else if (cmd_split[0] == "Array")
            {
                if (dec_arr.IsMatch(cmd))
                {
                    HandleArray(cmd);
                    //Сплитами парсим массив и выделяем все значения
                    //Добавляем в variables и output
                }
                else return false;
            }
            else
            {
                if (dec_var.IsMatch(cmd))
                {
                    //Добавляем переменную и её значение в variables
                    //Добавляем в output данные
                    string var_name = cmd_split[0];
                    if (variable_type.ContainsKey(var_name))
                        return false;
                    int const_value = -1;
                    try {
                        if (char.IsDigit(cmd_split[2][0]))
                            const_value = Convert.ToInt32(ReplaceVariableToValue(cmd_split[2]));
                        else if (cmd_split[2].Remove(2) == "b'") // Binary number
                            const_value = Convert.ToInt32(cmd_split[2].Substring(2), 2);
                        else if (cmd_split[2].Remove(2) == "h'") //Hexadecimal number
                            const_value = Convert.ToInt32(cmd_split[2].Substring(2), 16);
                        else
                            throw new CompilationException("Число");
                    }
                    catch
                    {
                        throw new CompilationException("Допишіть будь ласка значення змінної у команді " + cmd);
                    }
                    var var_value = const_value;
                    if (var_name != "m")
                    {
                        if (Math.Pow(2, (double)m) - 1 < var_value)
                        {
                            return false;
                        }
                        else
                        {

                            variables_values.Add(var_name, var_value);
                            variable_type.Add(var_name, TYPE.vars);
                            variable_timeAdded.Add(var_name, Vars++);
                            AddToOutput(var_value, ref output_var);
                            
                            line++;

                        }
                    }
                    else
                    {
                        return false; // overflow
                    }
                    return true;
                }
                else
                    return false; // Ошибка в синтаксисе
                
            }
            return true;
            
        }

        private void HandleArray(string arr)
        {
            string[] chars = arr.Split(new char[] {  '[', ']', '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            foreach (var x in chars)
            {
                chars[i] = x.Replace(" ", "");  // Убираем лишние пробелы 
                i++;
            }
            string ArrayName = GetArrayName(chars[0]);

            if (variable_type.ContainsKey(ArrayName))
            {
                throw new CompilationException("Спроба створити массив з вже викоростаним именем");
            }
            
            decimal lenght = GetLenghtExp(chars[1]);
            variable_timeAdded.Add(ArrayName, Arrs);
            Arrs += (int)lenght;
            variable_type.Add(ArrayName, TYPE.arrs);
            int CntOfVal = chars.Length; // тут и ниже начианаются некоторые проблемы, ибо сплит почему-то иногда возвращает пустую строку
            if (chars.Last() == "")
                CntOfVal--;
            if (CntOfVal - 3 != lenght)
            {
                throw new CompilationException("не усі ячейки масива заповнені");
            }

            for (i = 3; i < CntOfVal; i++)
            {
                chars[i] = GetBinaryInt(Convert.ToInt32(ReplaceVariableToValue(chars[i])));
                AddToOutput(Convert.ToInt32(chars[i], 2),ref output_arr);
                line++;
            }

            

        }

        private string ReplaceVariableToValue(string str)
        {
            MatchCollection matches = var.Matches(str);
            if (matches.Count > 0)
            {
                try
                {
                    foreach (Match match in matches)
                        str = str.Replace(match.Value, variables_values[match.Value].ToString());
                }
                catch (Exception ex)
                {
                    throw new CompilationException("звертання до неіснуючої змінної");
                }
                
            }
            return str;
        }

        private string GetArrayName(string arr) //Подаем сюда chars[0]
        {
            string name = arr.Replace(" ", "");
            return name.Replace("Array", "");
         
        }

        private decimal GetLenghtExp(string arr) //Подаем сюда chars[1]
        {
            arr = arr.Replace(":0", "");
            arr = ReplaceVariableToValue(arr);
            return p.result(arr) + 1;
        }
        //Добавляет в файл памяти указанную переменную
        private bool AddToOutput(int i, ref string Output)
        {
            string str = GetBinaryInt(i);
            Output += str + "\n";
            return true;
        }
        
        // Возвращает бинарное число длинной m
        private string GetBinaryInt(int i)  
        {
            string str = Convert.ToString(i, 2);

            for (int j = str.Length; j < m; j++)
            {
                str = "0" + str;
            }
            return str;
        }
        // Возвращает бинарное адресс длиной 9 (Это не магическое число в мануале указанно, что максимальная длинна адреса 511 ( 2**9 - 1))
        public string GetBinaryAdress(string name)
        {
            int i = 0; // Адресс в десятичной системе
            name = name.Replace("\t", string.Empty);
            if ( !variable_type.ContainsKey(name))
            {
                throw new CompilationException(" спроба отримати адресу неіснуючої зміної");
            }

            switch (variable_type[name])
            {
                case TYPE.arrs:
                    i = Vars + Cons;
                    break;
                case TYPE.vars:
                    i = Cons;
                    break;
                case TYPE.cons:
                    break;
            }
            i += variable_timeAdded[name];

            string str = Convert.ToString(i, 2);

            for (int j = str.Length; j < 9; j++)
            {
                str = "0" + str;
            }
            return str;
        }
        public TYPE GetType(string name)
        {
            if (!variable_type.ContainsKey(name))
            {
                throw new CompilationException(" спроба отримати адресу неіснуючої зміної");
            }
            else
                return variable_type[name];
        }
        private string GetValue(int adress)
        {
            string[] out_split = output.Split('\n');
            if (adress >= out_split.Length)
            {
                throw new CompilationException("звертання до невиділенної пам'яті");
            }
            else
                return out_split[adress];
        }

        private string GetValue(string adress)
        {
            string[] out_split = output.Split('\n');
            if (Convert.ToInt32(adress, 2) >= out_split.Length)
            {
                throw new CompilationException("звертання до невиділенної пам'яті");
            }
            else
                return out_split[Convert.ToInt32(adress, 2)];
        }

        public bool AddAllConstFromCodeSection(string[] Code)
        {
            foreach (string x in Code)
            {
                if (x.Contains("LOAD_CA_A")) // Если эта команда, то добавлять значения в память небезопасно. Так что тут указывать исключительно 9-байтовые значения
                    continue;
                MatchCollection matches = expression.Matches(x);
                if (matches.Count > 0)
                {
                    try
                    {
                        foreach (Match match in matches)
                        {
                            int value = -1;
                            string name = match.Value;
                            try
                            {

                                if (match.Value.Remove(2) == "b'") // Binary number
                                    value = Convert.ToInt32(match.Value.Substring(2), 2);
                                else if (match.Value.Remove(2) == "h'") //Hexadecimal number
                                    value = Convert.ToInt32(match.Value.Substring(2), 16);
                                else
                                {
                                    value = (int)p.result(ReplaceVariableToValue(match.Value));
                                    name = name.Replace("\t", string.Empty);
                                    name = name.Replace(" ", string.Empty);
                                }
                            }
                            catch
                            {

                            }

                            if (ca.IsMatch(match.Value))
                                continue;
                            try
                            { 
                                variables_values.Add(name, value);
                                variable_type.Add(name, TYPE.cons);
                                variable_timeAdded.Add(name, Cons++);
                                AddToOutput(Convert.ToInt32(value), ref output_con);
                            }
                            catch (ArgumentException)
                            {
                                
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        throw new CompilationException("звертання до невиділенної пам'яті");
                    }

                }
            }
            return true;
        }
        

    }

    

}
