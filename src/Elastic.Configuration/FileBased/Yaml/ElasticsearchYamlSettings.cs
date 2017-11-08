using System.Collections.Generic;
using System.ComponentModel;
using SharpYaml.Serialization;

namespace Elastic.Configuration.FileBased.Yaml
{
	public class ElasticsearchYamlSettings : Dictionary<string, object>, IYamlSettings
	{
		[YamlMember("cluster.name", SerializeMemberMode.Content)]
		[DefaultValue(null)]
		public string ClusterName { get; set; }

		[YamlMember("node.name")]
		[DefaultValue(null)]
		public string NodeName { get; set; }

		[YamlMember("node.master")]
		[DefaultValue(null)]
		public bool? MasterNode { get; set; }

		[YamlMember("node.data")]
		[DefaultValue(null)]
		public bool? DataNode { get; set; }

		[YamlMember("node.ingest")]
		[DefaultValue(null)]
		public bool? IngestNode { get; set; }

		[YamlMember("bootstrap.memory_lock")]
		[DefaultValue(null)]
		public bool? MemoryLock { get; set; }

		[YamlMember("path.data")]
		[DefaultValue(null)]
		public string DataPath { get; set; }

		[YamlMember("path.logs")]
		[DefaultValue(null)]
		public string LogsPath { get; set; }

		[YamlMember("discovery.zen.ping.unicast.hosts")]
		[DefaultValue(null)]
		public List<string> UnicastHosts { get; set; }

		[YamlMember("node.max_local_storage_nodes")]
		[DefaultValue(null)]
		public int? MaxLocalStorageNodes { get; set; }

		[YamlMember("discovery.zen.minimum_master_nodes")]
		[DefaultValue(null)]
		public int? MinimumMasterNodes { get; set; }

		[YamlMember("network.host")]
		[DefaultValue(null)]
		public string NetworkHost { get; set; }

		[YamlMember("http.port")]
		[DefaultValue(null)]
		public string HttpPortString { get; set; }

		public int? HttpPort => int.TryParse(HttpPortString, out int port) ? port : (int?)null;

		[YamlMember("transport.tcp.port")]
		[DefaultValue(null)]
		public string TransportTcpPortString { get; set; }
		
		public int? TransportTcpPort => int.TryParse(TransportTcpPortString, out int port) ? port : (int?)null;
		
		[YamlMember("xpack.license.self_generated.type")]
		[DefaultValue(null)]
		public string XPackLicenseSelfGeneratedType { get; set; }
		
		[YamlMember("xpack.security.enabled")]
		[DefaultValue(null)]
		public bool? XPackSecurityEnabled { get; set; }

		[YamlMember("xpack.security.transport.ssl.enabled")]
		[DefaultValue(null)]
		public bool? XPackSecurityTransportEnabled { get; set; }

		[YamlMember("xpack.security.transport.ssl.certificate")]
		[DefaultValue(null)]
		public string XPackSecurityTransportSslCertificate { get; set; }

		[YamlMember("xpack.security.transport.ssl.key")]
		[DefaultValue(null)]
		public string XPackSecurityTransportSslKey { get; set; }

		[YamlMember("xpack.security.transport.ssl.certificate_authorities")]
		[DefaultValue(null)]
		public string[] XPackSecurityTransportSslCertificateAuthorities { get; set; }

		[YamlMember("xpack.security.http.ssl.enabled")]
		[DefaultValue(null)]
		public bool? XPackSecurityHttpEnabled { get; set; }

		[YamlMember("xpack.security.http.ssl.certificate")]
		[DefaultValue(null)]
		public string XPackSecurityHttpSslCertificate { get; set; }

		[YamlMember("xpack.security.http.ssl.key")]
		[DefaultValue(null)]
		public string XPackSecurityHttpSslKey { get; set; }

		[YamlMember("xpack.security.http.ssl.certificate_authorities")]
		[DefaultValue(null)]
		public string[] XPackSecurityHttpSslCertificateAuthorities { get; set; }
	}
}