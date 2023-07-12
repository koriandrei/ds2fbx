using Autodesk.Fbx;

namespace Ds3FbxSharp
{
    public abstract class Exporter<SoulsType, FbxType>
    {
        protected Exporter(MyExporter exporter, SoulsType soulsType)
        {
            MyExporter = exporter;
            Souls = soulsType;
        }

        protected Exporter(FbxObject owner, SoulsType soulsType)
        {
            Owner = owner;
            Souls = soulsType;
        }

        public MyExporter MyExporter { get; }
        public FbxObject Owner { get; }

        public SoulsType Souls { get; }

        public FbxType Fbx
        {
            get
            {
                if (cachedFbxObject == null) { cachedFbxObject = GenerateFbx(); }

                return cachedFbxObject;
            }
        }
        protected abstract FbxType GenerateFbx();

        private FbxType cachedFbxObject;
    }
}