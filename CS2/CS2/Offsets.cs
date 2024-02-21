using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CS2
{
    public class Offsets
    {
        //base
        public int viewMatrix = 0x1915930;
        //matrix possible
        //client.dll+1915CC0 
        //client.dll+1915930 


        public int localPlayer = 0x1736BB8;
        public int entityList = 0x1729348;

        //attributes
        public int teamNum = 0x3CB;
        public int jumpFlag = 0x3D4;
        public int health = 0x334;
        public int origin = 0xD60;
    }
}
