using System;

namespace QrF.Framework.Contract
{
    public class ModelBase
    {
        public ModelBase()
        {
            CreateTime = DateTime.Now;
        }

        public virtual int ID { get; set; }
        public virtual DateTime CreateTime { get; set; }
    }
}
