# AskToYourDocs
AskToYourDocs is a sample application that demonstrates how to ask questions about your documents using the OpenAI API. 
If you're dealing with multiple projects and numerous technical documents for each project, you might find the need for 
a tool like AskToYourDocs, which simplifies the process of querying your documents.
# Why AskToYourDocs?
Managing multiple projects often involves sifting through a plethora of technical documents. AskToYourDocs streamlines 
this process by enabling you to ask questions directly to your documents. While various Python projects might explore 
similar concepts, many of these are tailored towards developers who are comfortable setting up their own environments.
# Made Using Chat GPT
This project has been entirely created using ChatGPT through prompt engineering. I didn't write a single line of code, 
except for some UI design and code re-arrangement.

# How to Use
If you're not a developer or simply prefer not to compile and set up this project manually, you can conveniently download 
the installation package from the link provided below.

1. Set up your OpenAI key in the settings tab.
2. Add the documents you wish to include in your search by using the "Add Documents" tab.
3. Now, start asking your questions!
# The Concept
When adding documents to the application, it automatically divides them into chunks based on the model's maximum token capacity. 
For each block, it generates embeddings and corresponding text using the CharGPT API.

When you input a question, it gets transformed into an embedding and is then compared to the embeddings of the text fragments using 
cosine similarity. This process helps identify the most relevant text block. Once the nearest match is found, that specific text block is 
selected and passed to the completion call along with its closest matching text block.

# Disadvantages
There are certain limitations to consider with this approach:

<b>Spanning Blocks and Documents:</b> In certain cases, the answer to a question might span across two text blocks or even multiple documents. 
Unfortunately, the current method may not effectively handle such scenarios. Fine-tuning the model could potentially address this issue 
by improving its ability to comprehend and connect information across different sections.

<b>Token Limitations:</b> The method is constrained by token limitations, which restrict the amount of text that can be processed in a single interaction.
Consequently, lengthy passages may be truncated, leading to incomplete or inaccurate responses.

It's important to acknowledge these drawbacks and explore potential solutions, such as fine-tuning, to enhance the system's performance 
and address these challenges effectively.











