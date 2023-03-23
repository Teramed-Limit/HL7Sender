# HL7Sender

HL7Sender 是一個能夠動態產出自定義 HL7 訊息並將其傳送至指定 HL7 Server 的命令列執行檔。

## 參數

HL7Sender 接受以下參數：

- `IP`：目標 HL7 Server 的位址
- `Port`：目標 HL7 Server 的阜號
- `HL7 template file`：參考的自訂義 HL7 檔案
- `Json file`：參考的 JSON 檔案

## 設計字串方式

HL7Sender 採用 Functional Programming 的方式設計字串。以下是設計字串時需要遵從的規則：

- `{}`：字串起始符與結束符
- `[]`：照順序執行方法列表，以 `,` 分隔
- `()`：方法起始值，若為空值請設為 `null`

以下是一個範例：

```c#
MSH|^~\&|SENDING_APPLICATION|SENDING_FACILITY|RECEIVING_APPLICATION|RECEIVING_FACILITY|{[GetCurrentTimeStamp](yyyyMMddHHmmss)}||ORU^R01|{[GetSequenceNumber](Null)}|P|2.3
PID|||{[GetJsonProperty](patientId)}||{[GetJsonProperty](patientsName)}||{[GetJsonProperty](patientsBirthDate)}|{[GetJsonProperty](patientsSex)}
OBR|1|||^Document|||202303151300|||{[GetJsonProperty](referringPhysiciansName)}|||||202303151400|||F
{[GetJsonProperty,GenerateOBXPDFBase64](pdffilePath)}
```