using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IO.Milvus.Client;
using IO.Milvus.Param;
using IO.Milvus.Param.Collection;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using IO.Milvus.Param.Dml;

namespace Milvus
{
    public class MilvusMemoryStore : IMemoryStore, IDisposable
    {
        public MilvusMemoryStore(string host, int port)
        {
            this._milvusClient = new MilvusServiceClient(ConnectParam.Create(host, port));
            this._disposedValue = false;
        }

        public Task CreateCollectionAsync(string collectionName, CancellationToken cancel = default)
        {
            var rd = new Random(DateTime.Now.Second);
            var response = this._milvusClient.CreateCollection(
                CreateCollectionParam.Create(
                    collectionName: collectionName,
                    shardsNum: 2,
                    new List<FieldType>()
                    {
                        FieldType.Create(
                            "id",
                            IO.Milvus.Grpc.DataType.String,
                            isPrimaryLey: true,
                            isAutoID: false
                        ),
                        FieldType.Create(
                            "is_reference",
                            IO.Milvus.Grpc.DataType.String
                        ),
                        FieldType.Create(
                            "external_source_name",
                            IO.Milvus.Grpc.DataType.String
                        ),
                        FieldType.Create(
                            "description",
                            IO.Milvus.Grpc.DataType.String
                        ),
                        FieldType.Create(
                            "text",
                            IO.Milvus.Grpc.DataType.String
                        ),
                        FieldType.Create(
                            "additional_metadata",
                            IO.Milvus.Grpc.DataType.String
                        ),
                        FieldType.Create(
                            "embedding",
                            IO.Milvus.Grpc.DataType.FloatVector
                        )
                    }
                )
            );

            return Task.CompletedTask;
        }

        public Task DeleteCollectionAsync(string collectionName, CancellationToken cancel = default)
        {
            this._milvusClient.DropCollection(collectionName);
            return Task.CompletedTask;
        }

        public Task<bool> DoesCollectionExistAsync(string collectionName, CancellationToken cancel = default)
        {
            var response = this._milvusClient.HasCollection(collectionName);
            return Task.FromResult(response.Data);
        }

        public Task<MemoryRecord> GetAsync(string collectionName, string key, CancellationToken cancel = default)
        {
            var response = this._milvusClient.Query(QueryParam.Create(
                collectionName: collectionName,
                partitionNames: null,
                outFields: new List<string>() {"id", "is_reference", "external_source_name", "description", "text", "additional_metadata" },
                expr: $"id in [{key}]"
            ));

            return Task.FromResult(MemoryRecord.FromJson(json: JsonSerializer.Serialize(response.Data.FieldsData), embedding: Embedding<float>.Empty));
        }

        public IAsyncEnumerable<MemoryRecord> GetBatchAsync(string collectionName, IEnumerable<string> keys, CancellationToken cancel = default)
        {
            var response = this._milvusClient.Query(QueryParam.Create(
                collectionName: collectionName,
                partitionNames: null,
                outFields: new List<string>() {"id", "is_reference", "external_source_name", "description", "text", "additional_metadata" },
                expr: $"id in [{keys.ToArray()}]"
            ));

            // ...this is awkward
        }

        public IAsyncEnumerable<string> GetCollectionsAsync(CancellationToken cancel = default)
        {
            var response = this._milvusClient.ShowCollections(ShowCollectionsParam.Create(null));
            return response.Data.CollectionNames.ToAsyncEnumerable();
        }

        public Task<(MemoryRecord, double)?> GetNearestMatchAsync(string collectionName, Embedding<float> embedding, double minRelevanceScore = 0, CancellationToken cancel = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesAsync(string collectionName, Embedding<float> embedding, int limit, double minRelevanceScore = 0, CancellationToken cancel = default)
        {
            throw new System.NotImplementedException();
        }

        public Task RemoveAsync(string collectionName, string key, CancellationToken cancel = default)
        {
            var response = this._milvusClient.Delete(DeleteParam.Create(collectionName, $"id in [{key}]"));
            return Task.CompletedTask;
        }

        public Task RemoveBatchAsync(string collectionName, IEnumerable<string> keys, CancellationToken cancel = default)
        {
            var response = this._milvusClient.Delete(DeleteParam.Create(collectionName, $"id in [{keys.ToArray()}]"));
            return Task.CompletedTask;
        }

        public Task<string> UpsertAsync(string collectionName, MemoryRecord record, CancellationToken cancel = default)
        {
            var requestParam = this.PrepareData(collectionName, new[] { record });
            var r = this._milvusClient.Insert(requestParam);

            return Task.FromResult(record.Key);
        }

        public IAsyncEnumerable<string> UpsertBatchAsync(string collectionName, IEnumerable<MemoryRecord> records, CancellationToken cancel = default)
        {
            var requestParam = this.PrepareData(collectionName, records);
            var response = this._milvusClient.Insert(requestParam);

            return records.Select(r => r.Metadata.Id).ToAsyncEnumerable();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region protected ================================================================================

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this._milvusClient.Close();
                }

                this._disposedValue = true;
            }
        }

        #endregion
    
        #region private ================================================================================

        private readonly MilvusServiceClient _milvusClient;
        private bool _disposedValue;

        private InsertParam PrepareData(string collectionName, IEnumerable<MemoryRecord> records)
        {
            var ids = new List<string>();
            var reference_types = new List<bool>();
            var sources = new List<string>();
            var descriptions = new List<string>();
            var texts = new List<string>();
            var additionalMetadata = new List<string>();
            var embeddings = new List<List<float>>();

            foreach (var record in records)
            {
                ids.Add(record.Metadata.Id);
                reference_types.Add(record.Metadata.IsReference);
                sources.Add(record.Metadata.ExternalSourceName);
                descriptions.Add(record.Metadata.Description);
                texts.Add(record.Metadata.Text);
                additionalMetadata.Add(record.Metadata.AdditionalMetadata);
                embeddings.Add(record.Embedding.Vector.ToList());

            }

            return InsertParam.Create(collectionName: collectionName, partitionName: null,
                new List<Field>()
                {
                    Field.Create("id", ids),
                    Field.Create("is_reference", reference_types),
                    Field.Create("external_source_name", sources),
                    Field.Create("description", descriptions),
                    Field.Create("text", texts),
                    Field.Create("additional_metadata", additionalMetadata),
                    Field.CreateBinaryVectors("embedding", embeddings),
                });
        }

        #endregion
    }
}