﻿using System;

namespace Markdown
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(new Markdown().Render("_s __dsf__ f_"));
        }
    }
}
