using SIE.Domain;
using SIE.MetaModel;
using SIE.ObjectModel;
using SIE.Tech.Processs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.DashBoard.DashBoards.LineStatuss.DataEntitys
{

    /// <summary>
    /// 工序
    /// </summary>
     [RootEntity, Serializable]
    [CriteriaQuery]   
    [Label("工序")]
    [DisplayMember(nameof(Name))]
    public class TechProess:DataEntity
    {
        #region 编码 Code
        /// <summary>
        /// 编码
        /// </summary>
        [Required]
        [MaxLength(40)]
        [NotDuplicate]
        [Label("编码")]
        public static readonly Property<string> CodeProperty = P<TechProess>.Register(e => e.Code);

        /// <summary>
        /// 编码
        /// </summary>
        public string Code
        {
            get { return GetProperty(CodeProperty); }
            set { SetProperty(CodeProperty, value); }
        }
        #endregion

        #region 名称 Name
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [MaxLength(40)]
        [Label("名称")]
        public static readonly Property<string> NameProperty = P<TechProess>.Register(e => e.Name);

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return GetProperty(NameProperty); }
            set { SetProperty(NameProperty, value); }
        }
        #endregion




        internal class TechProessConfig : EntityConfig<TechProess>
        {
            /// <summary>
            /// 配置数据库的映射
            /// </summary>
            protected override void ConfigMeta()
            {
                Meta.MapTable("TECH_PROCESS").MapAllProperties();
                Meta.Property(TechProess.NameProperty).ColumnMeta.HasIndex();
                Meta.EnablePhantoms();
            }
        }


    }



}
