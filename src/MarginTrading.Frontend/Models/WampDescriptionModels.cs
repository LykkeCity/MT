using MarginTrading.Common.Documentation;

namespace MarginTrading.Frontend.Models
{
    public class MethodInfoModel
    {
        public MethodDocInfo[] Rpc { get; set; }
        public MethodDocInfo[] Topic { get; set; }
    }
}
