using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace botForTRPO.GameClasses
{
    public class ServerFixGame
    {
        public static bool mistaken = false;
        public static int taskReady = 0;
        public static int taskMaxCount = 5;
        public static Random r = new();

        public string getMathFunc()
        {
            int operandIndex = r.Next(1, 3);
            char operand = '+';
            int x = r.Next(0, 10);
            int y = r.Next(0, 10);

            switch (operandIndex)
            {
                case 2:
                    operand = '-';
                    break;
            }
            char[] chars = new char[]
            {
                Convert.ToChar(x),
                Convert.ToChar(y),
                operand
            };
            return new string(chars);
        }

        public void getAnswer(int userAnswer, string mathFunc)
        {
            int x = Convert.ToInt32(mathFunc[0]);
            char operand = mathFunc[1];
            int y = Convert.ToInt32(mathFunc[2]);
            int answer = 0;
            switch (operand)
            {
                case '+':
                    answer = x + y;
                    break;
                case '-':
                    answer = x - y;
                    break;
            }
            if (answer < 0)
                answer *= -1;
            if (answer > 10)
            {
                double divForString = answer / 10.0;
                answer = Convert.ToInt32(divForString.ToString()[2].ToString());
            }
        }
    }
}