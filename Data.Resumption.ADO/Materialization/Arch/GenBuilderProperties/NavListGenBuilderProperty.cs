using System.Reflection.Emit;

namespace Data.Resumption.ADO.Materialization.GenBuilderProperties
{
    internal class NavListGenBuilderProperty : IGenBuilderProperty
    {
        public bool Singular => true;
        public void InstallFields(TypeBuilder type)
        {
            throw new System.NotImplementedException();
        }

        public void InstallProcessingLogic(GenProcessRowContext cxt)
        {
            throw new System.NotImplementedException();
        }

        public void InstallPushValue(GenInstanceMethodContext cxt)
        {
            throw new System.NotImplementedException();
        }
    }
}
