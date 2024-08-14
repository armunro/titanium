﻿using System.ClientModel;
using System.Text;
using OpenAI;
using OpenAI.Chat;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Document;
using Titanium.Samples;

namespace Titanium.Adapters.Generators;

public class GptAspectGenerator : IAspectGenerator
{
    readonly ConfigManager _config;
    readonly OpenAIClient _client;
    string _aspect;
    string _variant;
    string _extension;


    public GptAspectGenerator(ConfigManager config, OpenAIClient client)
    {
        _config = config;
        _client = client;
    }

    public void SetSource(string aspect, string variant, string extension)
    {
        _aspect = aspect;
        _variant = variant;
        _extension = extension;
    }


    public List<BaseAspect> GenerateAspects(Doc doc, string masterFilePath)
    {
        List<BaseAspect> aspects = new();


        string[] aspectVariantPaths = _config.Pathfinder.GetAspectFilePaths(doc.Project, doc.Id, _aspect, _extension);

        foreach (string aspectVariantPath in aspectVariantPaths)
        {
            string test = Path.GetFileNameWithoutExtension(aspectVariantPath);
            ClientResult<ChatCompletion> completeChat = _client.GetChatClient("gpt-3.5-turbo-16k")
                .CompleteChat(
                    new UserChatMessage("Using the json model provided, convert raw text to match the model: \n" +
                                        ReceiptSamples.RECEIPT_MODEL_GPT),
                    new UserChatMessage("Convert this raw text to json using the object model provided above: " +
                                        File.ReadAllText(aspectVariantPath)));
            IEnumerator<ChatMessageContentPart> enumerator = completeChat.Value.Content.GetEnumerator();
            StringBuilder jsonOut = new();
            while (enumerator.MoveNext())
            {
                string text = enumerator.Current.Text;
                Console.WriteLine(text);
                jsonOut.Append(text);
            }

            aspects.Add(BaseAspect.NewJson(test, jsonOut.ToString(), "gpt"));
        }


        return aspects;
    }
}

public class GptTransformAspectGenerator : GptAspectGenerator
{
    public GptTransformAspectGenerator(ConfigManager config, OpenAIClient client) : base(config, client)
    {
    }
}