using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Markdig.Extensions.PlantUmlRender
{
    class PlantUmlRenderExtension : IMarkdownExtension
    {
        private string plantUmlServerUrl;

        public PlantUmlRenderExtension(string plantUmlServerUrl)
        {
            this.plantUmlServerUrl = plantUmlServerUrl;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var htmlRenderer = renderer as HtmlRenderer;
            if (htmlRenderer != null)
            {
                var codeRenderer = htmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();
                codeRenderer.PlantUmlServerUrl = this.plantUmlServerUrl;
            }
        }
    }
}
