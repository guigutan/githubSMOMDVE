using SIE.Andon;
using SIE.Modules;

[assembly: Module(typeof(Module))]
namespace SIE.Andon
{
    /// <summary>
    /// 模块配置
    /// </summary>
    public class Module : DomainModule
    {
        /// <summary>
        /// 模块初始化
        /// </summary>
        /// <param name="app"></param>
        public override void Initialize(IApp app)
        {
            
        }
    }
}
