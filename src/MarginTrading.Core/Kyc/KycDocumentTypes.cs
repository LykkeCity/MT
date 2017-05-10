using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Core.Kyc
{
    public static class DocumentStates
    {
        public const string Uploaded = "Uploaded";
        public const string Approved = "Approved";
        public const string Declined = "Declined";
    }

    public interface IKycDocument
    {
        string ClientId { get; }
        string DocumentId { get; }
        string Type { get; }
        string Mime { get; }
        string KycComment { get; }
        string State { get; }

        string FileName { get; }
        DateTime DateTime { get; }
    }


    public class KycDocument : IKycDocument
    {
        public string ClientId { get; set; }
        public string DocumentId { get; set; }
        public string Type { get; set; }
        public string Mime { get; set; }
        public string KycComment { get; set; }
        public string State { get; set; }
        public string FileName { get; set; }
        public DateTime DateTime { get; set; }

        public static KycDocument Create(string clientId, string type, string mime, string fileName)
        {
            return new KycDocument
            {
                ClientId = clientId,
                Type = type,
                Mime = mime,
                DateTime = DateTime.UtcNow,
                FileName = fileName,
                State = DocumentStates.Uploaded
            };
        }
    }

    public static class KycDocumentTypes
    {
        public const string IdCard = "IdCard";
        public const string ProofOfAddress = "ProofOfAddress";
        public const string Selfie = "Selfie";


        public static IEnumerable<string> GetAllTypes()
        {
            yield return IdCard;
            yield return ProofOfAddress;
            yield return Selfie;
        }

        public static bool HasDocumentType(this string type)
        {
            return GetAllTypes().FirstOrDefault(itm => itm == type) != null;
        }

        public static bool HasType(this IEnumerable<IKycDocument> documents, string type)
        {
            var doc = documents.FirstOrDefault(itm => itm.Type == type);
            return doc != null && doc.State != DocumentStates.Declined;
        }


        public static bool HasAllTypes(this IEnumerable<IKycDocument> documents)
        {
            var docs = documents as IKycDocument[] ?? documents.ToArray();

            var idCard = docs.FirstOrDefault(itm => itm.Type == IdCard);
            var proof = docs.FirstOrDefault(itm => itm.Type == ProofOfAddress);
            var selfie = docs.FirstOrDefault(itm => itm.Type == Selfie);

            return idCard != null && idCard.State != DocumentStates.Declined
                   && proof != null && proof.State != DocumentStates.Declined
                   && selfie != null && selfie.State != DocumentStates.Declined;
        }

    }
}
