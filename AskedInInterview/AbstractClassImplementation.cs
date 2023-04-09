using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteviewPrepare.Main.AskedInInterview
{
    public abstract class SampleAsbtractClass
    {
        public abstract string Name { get; }

        public abstract void Mehtod1();

        public void MethodWithImplementation()
        {
            Console.WriteLine("MethodWithImplementation non abstract");
        }
    }
    public class ImplementationOfAbstractClass : SampleAsbtractClass
    {
        //public override string Name => "Implemented Class";
        public override string Name { get { return "Implemented class"; } }

        public override void Mehtod1()
        {
            Console.WriteLine("Method1 ABS");

        }
        public void Mehtod2()
        {
            Console.WriteLine("Method2 Non ABS with Name ABS : " + Name);

        }
    }
}
