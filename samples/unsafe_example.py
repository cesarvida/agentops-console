# Unsafe example file to trigger security analyzers (TEST ONLY)

def dangerous():
    user_code = "print('exfiltrate')"
    eval(user_code)  # Dangerous: pattern `eval(` will be detected

# Simulated exposed API key (fake)
api_key = "sk_test_FAKEKEY12345678"
