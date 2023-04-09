using InteviewPrepare.Main.AskedInInterview;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteviewPrepare.Main
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hi .Net Core");

            ImplementationOfAbstractClass sampleInhert = new ImplementationOfAbstractClass();
            sampleInhert.Mehtod1();
            sampleInhert.Mehtod2();
            sampleInhert.MethodWithImplementation();




            Console.Read();
        }
    }
}
