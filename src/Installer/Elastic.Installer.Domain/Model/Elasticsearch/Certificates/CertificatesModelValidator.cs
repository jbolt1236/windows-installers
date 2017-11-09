using System.IO;
using System.Linq;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Certificates
{
	public class CertificatesModelValidator : AbstractValidator<CertificatesModel>
	{
		public CertificatesModelValidator()
		{
			RuleFor(c => c.TransportCertFile)
				.NotEmpty()
				.WithMessage("Transport Certificate is required")
				.When(m => !m.GenerateTransportCert);

			RuleFor(c => c.TransportCertFile)
				.Must(File.Exists)
				.WithMessage("Transport Certificate must exist")
				.When(m => !string.IsNullOrEmpty(m.TransportCertFile));

			RuleFor(c => c.TransportKeyFile)
				.NotEmpty()
				.WithMessage("Transport Key is required")
				.When(m => !m.GenerateTransportCert);

			RuleFor(c => c.TransportKeyFile)
				.Must(File.Exists)
				.WithMessage("Transport Key must exist")
				.When(m => !string.IsNullOrEmpty(m.TransportKeyFile));

			RuleFor(c => c.TransportCAFiles)
				.NotEmpty()
				.WithMessage("Transport Certificate Authorities is required")
				.When(m => !m.GenerateTransportCert);

			RuleForEach(c => c.TransportCAFiles)
				.Must(File.Exists)
				.WithMessage("Transport Certificate Authority must exist")
				.When(m => m.TransportCAFiles.Any());

			RuleFor(c => c.HttpCertFile)
				.Must(File.Exists)
				.WithMessage("HTTP Certificate must exist")
				.When(m => !string.IsNullOrEmpty(m.HttpCertFile));

			RuleFor(c => c.HttpKeyFile)
				.NotEmpty()
				.WithMessage("HTTP Key is required")
				.When(m => !string.IsNullOrEmpty(m.HttpKeyFile));

			RuleFor(c => c.HttpKeyFile)
				.Must(File.Exists)
				.WithMessage("HTTP Key must exist")
				.When(m => !string.IsNullOrEmpty(m.HttpKeyFile));

			RuleFor(c => c.HttpCAFiles)
				.NotEmpty()
				.WithMessage("HTTP Certificate Authorities is required")
				.When(m => !string.IsNullOrEmpty(m.HttpCertFile));

			RuleForEach(c => c.HttpCAFiles)
				.Must(File.Exists)
				.WithMessage("HTTP Certificate Authority must exist")
				.When(m => m.HttpCAFiles.Any());
		}
	}
}