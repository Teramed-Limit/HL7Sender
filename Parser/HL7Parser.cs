using HL7Sender.DataStructure;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using SendingPDFHL7.DataStructure;
using SendingPDFHL7.HL7Structure;

namespace HL7Sender.Parser;

public class HL7Parser
{
    private readonly bool _showEmptyFields;

    /// <summary>
    /// 建構
    /// </summary>
    /// <param name="showEmptyFields">是否顯示空白字段</param>
    public HL7Parser(bool showEmptyFields = false)
    {
        _showEmptyFields = showEmptyFields;
    }

    #region Hl7Parsing functions

    /// <summary>
    /// 處理HL7結構
    /// </summary>
    /// <param name="message">HL7訊息</param>
    public HL7TreeNode ParseHL7Message(string message)
    {
        var pipeParser = new PipeParser();
        var hl7Message = pipeParser.Parse(message);

        var treeNode = new HL7TreeNode(new Field { Name = hl7Message.GetStructureName() });
        var hl7Parser = new HL7Parser();
        hl7Parser.ProcessStructureGroup((AbstractGroup)hl7Message, treeNode);

        return treeNode;
    }

    /// <summary>
    /// 處理HL7 MSA結構
    /// </summary>
    /// <param name="message">HL7訊息</param>
    public (bool isSuccess, string message) ParseMSAMessage(string message)
    {
        // 找出Acknowledgment code
        // Acknowledgment code是一個二字母的代碼，它的具體含義如下：
        var field = ParseHL7Message(message).FindFirst("Acknowledgement code");
        switch (field.Value)
        {
            // AA（Application Accept）：表示消息已成功接收且已通過驗證，並已在目標系統中成功處理。
            case "AA":
                return (true, "發送成功");
            // AE（Application Error）：表示由於某些原因，消息無法在目標系統中處理。
            case "AE":
                return (false, "發送失敗，AE表示由於某些原因，");
            // AR（Application Reject）：表示消息被目標系統拒絕，可能是由於格式錯誤、缺少必需的信息或是安全問題等。
            case "AR":
                return (false, "發送失敗，AR表示消息被目標系統拒絕，可能是由於格式錯誤、缺少必需的信息或是安全問題等");
            default:
                Console.Out.WriteLine("未知的錯誤");
                return (false, "未知的錯誤");
        }
    }

    /// <summary>
    /// Processes a structure group.
    /// A structure group is, primarily, a group of segments.  This could either be the entire
    /// message or special segments that need to be grouped together.  An example of this is
    /// the result segments (OBR, OBX and NTE), these are grouped together in the model
    /// definition (e.g. REF_I12_RESULTS_NOTES).
    /// </summary>
    /// <param name="structureGroup">The structure group.</param>
    /// <param name="treeNode"></param>
    private void ProcessStructureGroup(AbstractGroup structureGroup, TreeNode<Field> treeNode)
    {
        foreach (string segName in structureGroup.Names)
        {
            foreach (IStructure struc in structureGroup.GetAll(segName))
            {
                ProcessStructure(struc, treeNode);
            }
        }
    }

    /// <summary>
    /// Processes the structure.
    /// A base structure can be either a segment, or segment group. This function
    /// determines which it is before passing it on.
    /// </summary>
    /// <param name="structure">The structure.</param>
    /// <param name="treeNode"></param>
    private void ProcessStructure(IStructure structure, TreeNode<Field> treeNode)
    {
        if (structure.GetType().IsSubclassOf(typeof(AbstractSegment)))
        {
            AbstractSegment seg = (AbstractSegment)structure;
            ProcessSegment(seg, treeNode);
        }
        else if (structure.GetType().IsSubclassOf(typeof(AbstractGroup)))
        {
            AbstractGroup structureGroup = (AbstractGroup)structure;
            ProcessStructureGroup(structureGroup, treeNode);
        }
        else
        {
            Console.WriteLine("Something went wrong, Not a segment or group");
        }
    }

    /// <summary>
    /// Processes the segment.
    /// Loops through all of the fields within the segment, and parsing them individually.
    /// </summary>
    /// <param name="segment">The segment.</param>
    /// <param name="parentTreeNode"></param>
    private void ProcessSegment(AbstractSegment segment, TreeNode<Field> parentTreeNode)
    {
        var segmentTreeNode = parentTreeNode.AddChild(new Field() { Name = segment.GetStructureName() });

        int dataItemCount = 0;

        for (int i = 1; i <= segment.NumFields(); i++)
        {
            dataItemCount++;
            IType[] dataItems = segment.GetField(i);
            foreach (IType item in dataItems)
            {
                ProcessField(item, segment.GetFieldDescription(i), dataItemCount.ToString(), segmentTreeNode);
            }
        }
    }

    /// <summary>
    /// Processes the field.
    /// Determines the type of field, before passing it onto the more specific parsing functions.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="fieldDescription">The field description.</param>
    /// <param name="fieldCount">The field count.</param>
    /// <param name="segmentTreeNode"></param>
    private void ProcessField(IType item, string fieldDescription, string fieldCount, TreeNode<Field> segmentTreeNode)
    {
        if (item.GetType().IsSubclassOf(typeof(AbstractPrimitive)))
        {
            ProcessPrimitiveField((AbstractPrimitive)item, fieldDescription, fieldCount, segmentTreeNode);
        }
        else if (item.GetType() == typeof(Varies))
        {
            ProcessVaries((Varies)item, fieldDescription, fieldCount, segmentTreeNode);
        }
        else if (item.GetType().GetInterfaces().Contains(typeof(IComposite)))
        {
            AbstractType dataType = (AbstractType)item;
            string desc = string.IsNullOrEmpty(dataType.Description) ? fieldDescription : dataType.Description;
            ProcessCompositeField((IComposite)item, desc, fieldCount, segmentTreeNode);
        }
    }

    /// <summary>
    /// Processes the primitive field.
    /// A primitive field is the most basic type (i.e. no composite fields).  This function retrieves the data
    /// and builds the node in the TreeListView.
    /// </summary>
    /// <param name="dataItem">The data item.</param>
    /// <param name="fieldDescription">The field description.</param>
    /// <param name="fieldCount">The field count.</param>
    /// <param name="segmentTreeNode"></param>
    private void ProcessPrimitiveField(AbstractPrimitive dataItem, string fieldDescription, string fieldCount,
        TreeNode<Field> segmentTreeNode)
    {
        string desc = fieldDescription == string.Empty ? dataItem.Description : fieldDescription;

        if (_showEmptyFields || !string.IsNullOrEmpty(dataItem.Value))
        {
            segmentTreeNode.AddChild(new Field() { Name = desc, Id = fieldCount, Value = dataItem.Value });
        }
    }

    /// <summary>
    /// Processes the varies.
    /// "Varies" are the data in the OBX segment, the sending application can set the type hence generically the OBX
    /// value field is a variant type.
    /// The "Varies" data parameter contains the data in type IType (hence being passed back to process field).
    /// </summary>
    /// <param name="varies">The varies.</param>
    /// <param name="fieldDescription">The field description.</param>
    /// <param name="fieldCount">The field count.</param>
    /// <param name="segmentTreeNode"></param>
    private void ProcessVaries(Varies varies, string fieldDescription, string fieldCount,
        TreeNode<Field> segmentTreeNode)
    {
        ProcessField(varies.Data, fieldDescription, fieldCount, segmentTreeNode);
    }

    /// <summary>
    /// Processes the composite field.
    /// A composite field is a group of fields, such as "Coded Entry".
    /// This function breaks up the composite field and passes each field back to "ProcessField"
    /// </summary>
    /// <param name="composite">The composite.</param>
    /// <param name="fieldDescription">The field description.</param>
    /// <param name="fieldCount">The field count.</param>
    /// <param name="parentTreeNode"></param>
    private void ProcessCompositeField(IComposite composite, string fieldDescription, string fieldCount,
        TreeNode<Field> parentTreeNode)
    {
        var subParentNode = parentTreeNode.AddChild(new Field() { Name = fieldDescription, Id = fieldCount });

        int subItemCount = 0;
        foreach (IType subItem in composite.Components)
        {
            subItemCount++;
            ProcessField(subItem, string.Empty, subItemCount.ToString(), subParentNode);
        }
    }

    #endregion Hl7Parsing functions
}