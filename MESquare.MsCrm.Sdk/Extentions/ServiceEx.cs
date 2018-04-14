using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;

namespace MESquare.MsCrm.Sdk.Extentions
{
    public static class ServiceEx
    {
        public static IEnumerable<Entity> RetriveAllUsers( this IOrganizationService service, ColumnSet columnSet = null)
        {
            var query = new QueryExpression("systemuser");
            query.ColumnSet = columnSet ?? new ColumnSet{ AllColumns = true };

            return RetriveAll(service,query);
        }

        private static List<Entity> RetriveAll(IOrganizationService service, QueryExpression query)
        {
            var entities = new List<Entity>();
            EntityCollection result;
            var pagingCookie = null as String;
            do
            {
                result = service.RetrieveMultiple(query);
                pagingCookie = result.PagingCookie;
                entities.AddRange(result.Entities);

            } while (result.MoreRecords);

            return entities;
        }

        public static EntityMetadataCollection RetrieveAuditableEntities(this IOrganizationService service)
        {
            var filter = new MetadataFilterExpression(LogicalOperator.And);
            filter.Conditions.Add(new MetadataConditionExpression("IsAuditEnabled", MetadataConditionOperator.Equals, true));

            var request = new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression()
                {
                    Criteria = filter
                },
                ClientVersionStamp = null,
            };
            
            var response = service.Execute(request) as RetrieveMetadataChangesResponse;

            return response.EntityMetadata;
        }

        public static List<Entity> RetrieveAuditData(this IOrganizationService service, AuditDataFilter filter)
        {
            var query = new QueryExpression("audit")
            {
                ColumnSet = new ColumnSet { AllColumns = true }
            };
            query.Criteria.AddCondition("createdon", ConditionOperator.GreaterEqual, filter.CreatedAfter);
            query.Criteria.AddCondition("createdon", ConditionOperator.LessEqual, filter.CreatedBefore);

            if (filter.Entities.Count > 0)
                query.Criteria.AddCondition("objecttypecode", ConditionOperator.In,filter.Entities.Select(e => e.EntityLogicalName).ToArray());
            if (filter.Author.HasValue)
                query.Criteria.AddCondition("userid", ConditionOperator.Equal, filter.Author.Value);
            if( filter.ObjectId.HasValue )
                query.Criteria.AddCondition("objectid", ConditionOperator.Equal, filter.ObjectId.Value);

            if (filter.Actions != null && filter.Actions.Count > 0)
                query.Criteria.AddCondition(new ConditionExpression("action", ConditionOperator.In, filter.Actions.ToArray()));
            if (filter.ExcludeActions != null && filter.ExcludeActions.Count > 0)
                query.Criteria.AddCondition(new ConditionExpression("action", ConditionOperator.NotIn, filter.ExcludeActions.ToArray()));

            query.PageInfo.Count = filter.Take;
            query.PageInfo.PageNumber = Convert.ToInt32(Math.Floor(filter.Skip*1.0 /filter.Take)) + 1;
            query.PageInfo.PagingCookie = filter.PagingCookie;
            query.PageInfo.ReturnTotalRecordCount = String.IsNullOrWhiteSpace(filter.PagingCookie);
            query.AddOrder("createdon", OrderType.Descending);

            var entityCollection = service.RetrieveMultiple(query);
            filter.PagingCookie = entityCollection.PagingCookie;

            return entityCollection.Entities.ToList();
        }

        #region RetriveAuditDetails

        public static List<AuditDetail> RetrieveAuditDetails(this IOrganizationService service,AuditDataFilter filter)
        {
            var audits = default(List<Entity>);
            var details = new List<AuditDetail>();
            var index = 0;

            do
            {
                filter.Skip = filter.Take * index;

                audits = service.RetrieveAuditData(filter);
                details.AddRange(service.RetrieveAuditDetails(audits, filter));

                index++;
            }
            while (audits.Count > 0 && details.Count < filter.NumberOfRecords);

            return details.Take(filter.NumberOfRecords).ToList();
        }

        public static void ExpandAuditDetails(
            this IOrganizationService service,
            AuditDataFilter filter,
            Action<List<AuditDetail>> action,
            Func<IOrganizationService> newOrganizationService = null)
        {
            var audits = default(List<Entity>);
            var detailsTmp = new List<AuditDetail>();
            var count = 0;
            var index = 0;

            do
            {
                filter.Skip = filter.Take * index;
                audits = service.RetrieveAuditData(filter);
                detailsTmp = service.RetrieveAuditDetails(audits, filter, newOrganizationService);
                action(detailsTmp);
                count += detailsTmp.Count;
                index++;
            }
            while (audits.Count > 0 && count < filter.NumberOfRecords);

        }

        private static List<AuditDetail> RetrieveAuditDetails(this IOrganizationService service, List<Entity> audits, AuditDataFilter filter, Func<IOrganizationService> newOrganizationProxy = null)
        {
            if (newOrganizationProxy == null)
                return service.RetrieveAuditDetailsSync(audits, filter);
            else
                return service.RetrieveAuditDetailsAsync(audits, filter, newOrganizationProxy);
        }

        private static List<AuditDetail> RetrieveAuditDetailsSync(this IOrganizationService service, List<Entity> audits, AuditDataFilter filter)
        {
            var step = filter.NumberOfRequestsPerBulkRequest;
            var auditRecords = new List<AuditDetail>();

            for (var i = 0; i < (Math.Ceiling(audits.Count * 1.0 / step)); i++)
            {
                var currentAudits = audits.Skip(i * step).Take(step).ToList();
                var request = service.RetriveAuditDetailsRequest(currentAudits);
                var response = service.Execute(request) as ExecuteMultipleResponse;

                auditRecords.AddRange(service.RetrieveAuditDetailsRecords(response, filter));
            }

            return auditRecords;
        }

        private static List<AuditDetail> RetrieveAuditDetailsAsync(
            this IOrganizationService service, 
            List<Entity> audits, 
            AuditDataFilter filter, 
            Func<IOrganizationService> newOrganizationService)
        {
            var step = 300;
            var auditRecords = new List<AuditDetail>();

            Parallel.For(0, Convert.ToInt32((Math.Ceiling(audits.Count * 1.0 / step))),
                new ParallelOptions { MaxDegreeOfParallelism = filter.MaxNumberOfParallelRequests },
                newOrganizationService,
                (index, loopState, proxy) =>
                {

                    var currentAudits = audits.Skip(Convert.ToInt32(index * step)).Take(step).ToList();
                    var request = proxy.RetriveAuditDetailsRequest(currentAudits);
                    var response = proxy.Execute(request) as ExecuteMultipleResponse;
                    var newAuditRecords = service.RetrieveAuditDetailsRecords(response, filter);

                    lock (auditRecords)
                    {
                        auditRecords.AddRange(newAuditRecords);
                    }

                    return proxy;
                },
                (proxy) => { }
            );

            return auditRecords;
        }

        private static ExecuteMultipleRequest RetriveAuditDetailsRequest( this IOrganizationService service, List<Entity> audits)
        {
            var request = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = false,
                    ReturnResponses = true,
                },
                Requests = new OrganizationRequestCollection()
            };
            request.Requests.AddRange(audits.Select(audit => new RetrieveAuditDetailsRequest { AuditId = audit.Id }));
            
            return request;
        }


        private static List<AuditDetail> RetrieveAuditDetailsRecords(this IOrganizationService service, ExecuteMultipleResponse response,AuditDataFilter filter)
        {
            var auditRecords = new List<AuditDetail>();
            foreach (var filterEntity in filter.Entities)
            {
                var attributeAuditResponses = response.Responses
                    .Select(r => (r.Response as RetrieveAuditDetailsResponse).AuditDetail as AttributeAuditDetail)
                    .Where(r =>
                        r != null
                        && (
                            r.OldValue?.LogicalName == filterEntity.EntityLogicalName
                            || r.NewValue?.LogicalName == filterEntity.EntityLogicalName
                        )
                    ).ToList();

                if (!filterEntity.AllAttributes)
                {


                    attributeAuditResponses.ForEach(attr =>
                    {
                        if( attr.OldValue != null )
                            attr.OldValue.Attributes.ToList().ForEach(oa => {
                                if (!filterEntity.Attributes.Contains(oa.Key))
                                    attr.OldValue.Attributes.Remove(oa.Key);
                           });

                        if( attr.NewValue != null )
                            attr.NewValue.Attributes.ToList().ForEach(na => {
                                if (!filterEntity.Attributes.Contains(na.Key))
                                    attr.NewValue.Attributes.Remove(na.Key);
                            });

                        if( (attr.OldValue?.Attributes.Count ?? 0) > 0 || (attr.NewValue?.Attributes.Count ?? 0) > 0 )
                            auditRecords.Add(attr);

                    });
                }
                else
                {
                    //auditRecords.AddRange(response.Responses.Select(r => (r.Response as RetrieveAuditDetailsResponse).AuditDetail));
                    auditRecords.AddRange(attributeAuditResponses);
                }
            }

            return auditRecords;
        }

        #endregion
    }
}
