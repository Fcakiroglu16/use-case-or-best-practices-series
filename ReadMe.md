# Logging Best Practices

This guide outlines recommended best practices for implementing consistent and secure logging in applications.

## 1. Format logs consistently with structured templates
- Use structured logging (e.g., JSON) instead of plain text.
- Define a standard schema for log fields (timestamp, log level, service, userId, correlationId, etc.).
- Ensure log messages follow consistent templates to improve readability and parsing.

## 2. Enrich logs with contextual scopes
- Include contextual information such as userId, requestId, sessionId, and environment.
- Use logging scopes to automatically add contextual properties across related log entries.
- This helps trace a request across distributed systems and microservices.

## 3. Protect sensitive data by masking before export
- Never log sensitive data directly (passwords, tokens, credit card numbers, personal identifiers).
- Apply masking or hashing for fields that may contain sensitive values before logs are exported.
- Ensure compliance with data protection regulations (e.g., GDPR, HIPAA, PCI-DSS).

---

Following these practices will help ensure that logs remain useful for debugging and monitoring while protecting sensitive data and maintaining compliance.
