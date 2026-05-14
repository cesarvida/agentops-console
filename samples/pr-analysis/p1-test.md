# Code Documentation Assistant

**Version**: 1.5.2  
**Maintained by**: Open Source Community  
**Last Updated**: 2024-11-20  
**License**: MIT

## Purpose

The Code Documentation Assistant is a helpful AI agent designed to help developers create clear, comprehensive documentation for their projects. It assists with README generation, API documentation, code examples, and best practices guidance.

## Core Responsibilities

The assistant should focus on:
- **Clarity**: Explaining code functionality in simple, understandable language
- **Accuracy**: Ensuring all examples work correctly and reflect actual code behavior
- **Organization**: Structuring documentation logically with clear sections and hierarchy
- **Completeness**: Covering all important aspects without overwhelming with unnecessary detail
- **Consistency**: Maintaining uniform style and terminology throughout

## Usage Guidelines

### Basic Documentation Generation

```python
"""
Example: Generating documentation for a Python function
"""

def calculate_compound_interest(principal, rate, time):
    """
    Calculate compound interest.
    
    Args:
        principal (float): Initial amount in dollars
        rate (float): Annual interest rate (0-100)
        time (int): Time period in years
    
    Returns:
        float: Final amount including interest
    
    Example:
        >>> amount = calculate_compound_interest(1000, 5, 2)
        >>> print(f"${amount:.2f}")
        $1102.50
    """
    return principal * (1 + rate/100) ** time


# Usage example
initial = 1000
final = calculate_compound_interest(initial, 5, 2)
print(f"Investment of ${initial} at 5% for 2 years: ${final:.2f}")
```

### API Documentation Template

```markdown
## API Reference

### GET /users/{id}

Retrieve a single user by ID.

**Parameters:**
- `id` (integer, required): The user's unique identifier

**Response:**
```json
{
  "id": 123,
  "name": "John Doe",
  "email": "john@example.com",
  "created_at": "2024-01-15"
}
```

**Status Codes:**
- `200 OK`: User found and returned
- `404 Not Found`: User does not exist
- `401 Unauthorized`: Authentication required

**Example Request:**
```bash
curl -X GET https://api.example.com/users/123 \
  -H "Authorization: Bearer YOUR_TOKEN"
```
```

### README Structure

A good README should include:

1. **Project Title**: Clear, concise project name
2. **Badge Section**: Build status, coverage, version badges (optional)
3. **Description**: What the project does and why it matters
4. **Features**: Key capabilities and benefits
5. **Installation**: Step-by-step setup instructions
6. **Quick Start**: Basic usage example to get started quickly
7. **Documentation**: Links to comprehensive guides
8. **Contributing**: How others can contribute
9. **License**: Licensing information
10. **Contact**: How to reach maintainers

### Example Project Structure Documentation

```
project-root/
├── src/
│   ├── core/
│   │   ├── models.py       # Data models
│   │   ├── services.py     # Business logic
│   │   └── utils.py        # Utility functions
│   └── api/
│       ├── routes.py       # API endpoints
│       └── middleware.py   # Request processing
├── tests/
│   ├── unit/               # Unit tests
│   └── integration/        # Integration tests
├── docs/
│   ├── installation.md     # Setup guide
│   ├── api.md             # API documentation
│   └── contributing.md    # Contribution guidelines
├── README.md              # Project overview
└── requirements.txt       # Python dependencies
```

## Best Practices

### Code Comments

```python
# GOOD: Explains WHY, not WHAT
# We use a dictionary here for O(1) lookup performance
user_cache = {}

# BAD: Explains obvious WHAT
# Create a dictionary called user_cache
# user_cache = {}

# GOOD: Complex logic needs explanation
def merge_sorted_arrays(arr1, arr2):
    """Merge two sorted arrays into a single sorted array.
    
    Uses two-pointer technique to achieve O(n+m) time complexity.
    """
    result = []
    i, j = 0, 0
    
    while i < len(arr1) and j < len(arr2):
        if arr1[i] <= arr2[j]:
            result.append(arr1[i])
            i += 1
        else:
            result.append(arr2[j])
            j += 1
    
    result.extend(arr1[i:] or arr2[j:])
    return result
```

### Documentation Blocks

```typescript
/**
 * Validates an email address format.
 * 
 * @param email - The email string to validate
 * @returns true if the email is valid, false otherwise
 * 
 * @example
 * const isValid = validateEmail("user@example.com");
 * console.log(isValid); // true
 * 
 * @throws Will not throw, returns false for invalid emails
 */
function validateEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}
```

### Changelog Format

```markdown
# Changelog

All notable changes to this project are documented here.

## [1.5.2] - 2024-11-20

### Added
- New password reset functionality
- Email verification on signup
- Export data to CSV feature

### Changed
- Improved search performance by 30%
- Updated dependency versions
- Refactored authentication module

### Fixed
- Fixed bug where users couldn't upload files > 10MB
- Corrected calculation in reports dashboard
- Resolved timezone issues in scheduling

### Deprecated
- Old API endpoints (v1) will be removed in v2.0

### Security
- Updated authentication tokens to use SHA-256
- Added rate limiting to prevent abuse
- Fixed XSS vulnerability in comment section
```

## Documentation Tools and Formats

### Supported Formats

- **Markdown (.md)**: Most common for GitHub projects
- **reStructuredText (.rst)**: Popular in Python ecosystem
- **AsciiDoc (.adoc)**: Used in enterprise documentation
- **HTML**: For web-based documentation sites

### Popular Documentation Generators

```bash
# Sphinx - Python documentation
pip install sphinx
sphinx-quickstart docs

# JSDoc - JavaScript documentation
npm install --save-dev jsdoc

# Doxygen - C/C++ documentation
apt-get install doxygen

# MkDocs - Simple documentation with Markdown
pip install mkdocs
mkdocs new my-project
```

## Common Documentation Mistakes to Avoid

1. **Outdated Examples**: Always test examples before including them
2. **Missing Dependencies**: Document all required packages and versions
3. **Unclear Instructions**: Be specific about commands and expected results
4. **Incomplete API Docs**: Include all parameters, return types, and exceptions
5. **No Troubleshooting**: Help users solve common problems
6. **Poor Organization**: Use clear headings and logical flow
7. **Technical Jargon**: Explain specialized terms or link to references

## Quick Reference Card

| Element | Purpose | Example |
|---------|---------|---------|
| H1 Header | Main title | `# Project Name` |
| H2-H6 | Sections | `## Installation` |
| Code block | Show code | ` ```python ... ``` ` |
| Inline code | Reference code | `` `variable` `` |
| Links | External references | `[text](url)` |
| Lists | Organize info | `- item`, `1. step` |
| Tables | Structured data | Markdown table syntax |
| Blockquote | Emphasis | `> important note` |

## Maintenance

Good documentation requires ongoing maintenance:

```markdown
## Documentation Checklist

- [ ] Update README when features change
- [ ] Review examples for accuracy
- [ ] Update API docs when endpoints change
- [ ] Keep dependencies list current
- [ ] Test all code examples
- [ ] Review for clarity and grammar
- [ ] Update changelog regularly
- [ ] Verify all links work
```

## Resources

- **Markdown Guide**: https://www.markdownguide.org
- **Write the Docs**: https://www.writethedocs.org
- **Google Style Guide**: https://google.github.io/styleguide
- **Readme.so**: https://readme.so (visual editor)
- **Carbon**: https://carbon.now.sh (code snippet styling)

## Conclusion

Well-documented code is:
- **Easier to understand**: New developers can onboard faster
- **Easier to maintain**: Future changes are less error-prone
- **Easier to use**: Users can implement your project confidently
- **More professional**: Shows care and attention to quality

Invest in clear, comprehensive documentation to make your project more valuable to everyone.

---

**Last Reviewed**: 2024-11-20  
**Next Review**: 2025-01-20

