using SIE.Domain;
using SIE.Domain.Validation;
using SIE.MetaModel;
using SIE.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIE.MES.DashBoard.KzBoard.RegionBoards
{
    /// <summary>
    /// 区域与产线的关联关系
    /// </summary>
    [RootEntity, Serializable]
    [Label("区域与产线的关联关系")]
    [CriteriaQuery]
    public class RegionBoard : DataEntity
    {
        #region 看报区域 Region
        /// <summary>
        /// 看报区域
        /// </summary>
        [Label("看报区域")]
        public static readonly Property<string> RegionProperty = P<RegionBoard>.Register(e => e.Region);

        /// <summary>
        /// 看报区域
        /// </summary>
        public string Region
        {
            get { return this.GetProperty(RegionProperty); }
            set { this.SetProperty(RegionProperty, value); }
        }
        #endregion

        #region 产线明细 RegionBoardDetailList
        /// <summary>
        /// 产线明细
        /// </summary>
        [Label("产线明细")]
        public static readonly ListProperty<EntityList<RegionBoardDetail>> RegionBoardDetailListProperty = P<RegionBoard>.RegisterList(e => e.RegionBoardDetailList);

        /// <summary>
        /// 产线明细
        /// </summary>
        public EntityList<RegionBoardDetail> RegionBoardDetailList
        {
            get { return this.GetLazyList(RegionBoardDetailListProperty); }
        }
        #endregion

    }

    internal class RegionBoardConfig : EntityConfig<RegionBoard>
    {
        protected override void AddValidations(IValidationDeclarer rules)
        {
            rules.AddRule(RegionBoard.RegionProperty, new NotDuplicateRule());
            base.AddValidations(rules);
        }

        protected override void ConfigMeta()
        {
            Meta.MapTable("REGION_BOARD").MapAllProperties();
            Meta.EnableInvOrg();
            Meta.EnablePhantoms();
        }
    }
}
