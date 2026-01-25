using SIE.Domain;
using SIE.MES.ProcessProperty;
using SIE.MetaModel;
using SIE.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.DashBoard.DashBoards.LineStatuss.DataEntitys
{

    [RootEntity, Serializable]
    [ConditionQueryType(typeof(ProcessPtyCriterial))]
    [Label("工序属性维护")]
    public class ProcessPty : DataEntity
    {
        /// <summary>
        /// 工序Id
        /// </summary>
        [Label("工序Id")]
        public static readonly IRefIdProperty ProcessIdProperty = P<ProcessPty>.RegisterRefId(e => e.ProcessId, ReferenceType.Normal);

        /// <summary>
        /// 工序Id
        /// </summary>
        public double ProcessId
        {
            get { return (double)GetRefNullableId(ProcessIdProperty); }
            set { SetRefNullableId(ProcessIdProperty, value); }
        }

        /// <summary>
        /// 工序
        /// </summary>
        public static readonly RefEntityProperty<TechProess> TechProessProperty = P<ProcessPty>.RegisterRef(e => e.TechProess, ProcessIdProperty);

        /// <summary>
        /// 工序
        /// </summary>
        public TechProess TechProess
        {
            get { return GetRefEntity(TechProessProperty); }
            set { SetRefEntity(TechProessProperty, value); }
        }


      
        
                
        
        public class ProcessPtyConfig : EntityConfig<ProcessPty>
        {
            protected override void ConfigMeta()
            {
                Meta.MapTable("PROCESS_PTY").MapAllProperties();
                Meta.EnablePhantoms();
            }
        }
       
    }

}
