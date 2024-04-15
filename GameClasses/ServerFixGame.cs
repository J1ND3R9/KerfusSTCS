using botForTRPO.Models;
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
        public bool mistaken = false;
        public int taskReady = 0;
        public int taskMaxCount = 1;
        public static Satellite satellite { get; set; }
        public static Random r = new();

        public ServerFixGame(Satellite s)
        {
            satellite = s;
        }

        public Satellite getSatellite() { return satellite; }
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

            char x1 = Convert.ToChar(x.ToString());
            char y1 = Convert.ToChar(y.ToString());
            char[] chars =
            {
                x1,
                operand,
                y1
            };
            return new string(chars);
        }

        public void getAnswer(int userAnswer, string mathFunc)
        {
            int x = Convert.ToInt32(mathFunc[0].ToString());
            char operand = mathFunc[1];
            int y = Convert.ToInt32(mathFunc[2].ToString());
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
            if (answer == userAnswer)
                return;
            else
                mistaken = true;
        }
    }
}