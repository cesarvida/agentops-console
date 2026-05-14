"""
Simple LLM interaction script.
Sends a prompt to OpenAI API and returns the response.
"""

import openai

def ask_llm(prompt: str) -> str:
    client = openai.OpenAI()
    response = client.chat.completions.create(
        model="gpt-4",
        messages=[{"role": "user", "content": prompt}]
    )
    return response.choices[0].message.content

if __name__ == "__main__":
    result = ask_llm("Explain quantum computing simply")
    print(result)
