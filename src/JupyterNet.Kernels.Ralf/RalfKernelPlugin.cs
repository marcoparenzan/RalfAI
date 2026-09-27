using JupyterNet.Kernels.Abstractions;

namespace JupyterNet.Kernels.Ralf;

/// <summary>The entire surface JupyterNet.Host needs to load this plugin — see IKernelPlugin.</summary>
public sealed class RalfKernelPlugin : IKernelPlugin
{
    public string KernelId => "ralf";

    public IKernel CreateKernel(INotebookHost notebookHost) => new RalfKernel(notebookHost);
}
