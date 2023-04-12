using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lemegeton.Core
{

    public abstract class CustomPropertyInterface
    {

        public abstract void RenderEditor(string path);
        public abstract string Serialize();
        public abstract void Deserialize(string data);

    }

}
