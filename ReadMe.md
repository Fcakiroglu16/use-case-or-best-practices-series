# 🎯 Hybrid Search System

This project combines **Full-Text Search** (Lexical) and **Semantic Search** methods to provide a powerful search experience.

---

## 📋 Table of Contents

- [Technologies](#-technologies)
- [System Architecture](#-system-architecture)
- [Data Flows](#-data-flows)
  - [1. Data Ingestion and Indexing](#1-data-ingestion-and-indexing)
  - [2. Hybrid Search Query](#2-hybrid-search-query)
- [Benefits](#-benefits-of-this-design)

---

## 🛠️ Technologies

| Technology | Purpose |
|-----------|---------|
| **ASP.NET Core API** | Main backend service, data ingestion and result fusion |
| **Python (FastAPI/Flask)** | AI service, vector generation and semantic search |
| **Elasticsearch** | Full-text search (Lexical Search) |
| **Pinecone** | Vector database (Semantic Search) |
| **RabbitMQ** | Message queue, asynchronous communication |
| **SentenceTransformers** | Embedding model (e.g., `all-mpnet-base-v2`, `paraphrase-multilingual-mpnet-base-v2`) |

---

## 🏗️ System Architecture

This architecture consists of **two main flows**:

1. **Data Ingestion (Indexing)** - Storing articles in the system
2. **Querying (Search)** - Processing user queries

### Data Connection

- **Elasticsearch** → Stores text with `ArticleID`
- **Pinecone** → Stores vectors with `ArticleID`
- Both databases are linked through **ArticleID**

---

## 🔄 Data Flows

### 1. Data Ingestion and Indexing

Process steps when a new article is added to the system:

#### 📥 **Step 1: Entry Point**
- User or CMS sends a request to `POST /api/articles` endpoint
- Request Body: `{ "title": "...", "content": "..." }`

#### 💾 **Step 2: ASP.NET Core API - Data Storage**
- A unique `ArticleID` is generated (GUID)
- Article data (ArticleID, title, content, timestamp) is saved to **Elasticsearch**
- Elasticsearch automatically performs full-text indexing

#### 📤 **Step 3: ASP.NET Core API - Queue Publishing**
- The same data (ArticleID, title, content) is prepared as a message
- Message is sent to `article-indexing-exchange` in **RabbitMQ**

#### 🐍 **Step 4: Python AI Service - Message Consumption**
- Python service continuously listens to `embedding-queue`
- New article message is consumed from the queue

#### 🤖 **Step 5: Python AI Service - Embedding Generation**
- Using **SentenceTransformers** model:
  - `title` and `content` are combined (or just `content`)
  - Text is transformed into a single vector (embedding)

#### 🎯 **Step 6: Python AI Service - Vector Storage**
- Generated vector is `upserted` to **Pinecone** with the `ArticleID`

#### ✅ **Result**
- **Elasticsearch**: ArticleID → Text (for full-text search)
- **Pinecone**: ArticleID → Vector (for semantic search)

---

### 2. Hybrid Search Query

Process steps when a user performs a search:

#### 🔍 **Step 1: Query Entry Point (Orchestrator)**
- User: `GET /api/search?q={user_query}`
- **ASP.NET Core API** acts as the "Search Orchestrator"

#### ⚡ **Step 2: Parallel Search Initiation**

The API initiates **two simultaneous searches** for the query:

##### 🅰️ **Search Path A - Full-Text Search**
1. ASP.NET API → Sends query to **Elasticsearch**
2. Elasticsearch → Returns top 20 results matching keywords
3. Results: `ArticleID` + `BM25 score`

##### 🅱️ **Search Path B - Semantic Search**
1. ASP.NET API → Sends `POST /semantic-search` request to **Python AI Service**
2. Python Service:
   - Converts query text to vector using the same embedding model
   - Searches the vector in **Pinecone**
3. Pinecone → Returns top 20 semantically similar results
4. Results: `ArticleID` + `Similarity score`
5. Python Service → Returns result list to ASP.NET API

#### 🔀 **Step 3: Fusion and Re-Ranking**

ASP.NET API now has:
- **List A**: Elasticsearch results (with BM25 scores)
- **List B**: Pinecone results (with Cosine Similarity scores)

**Fusion Algorithm**: **Reciprocal Rank Fusion (RRF)**
- Fairly combines different scoring systems
- Assigns combined scores based on **rankings**, not scores
- Most effective hybrid search method

#### 📊 **Step 4: Final Results**
1. Top 10 ArticleIDs are selected after RRF re-ranking
2. Details (title, content, etc.) of these 10 ArticleIDs are fetched from **Elasticsearch**
3. **Merged, high-quality result list** is returned to the user

---

## 💡 Benefits of This Design

### ⚡ **Asynchronous Processing**
- Users don't wait thanks to RabbitMQ
- Embedding generation happens in the background
- API response time is short

### 🔗 **Loose Coupling (Decoupled)**
- .NET service doesn't know how embeddings are created
- Python service doesn't know how data is stored
- Each service can be developed and scaled independently

### 🎯 **Powerful Search Experience**
- **Full-Text**: "Java" search → Exact match
- **Semantic**: "object-oriented programming language" → Semantic match
- Two methods combine to provide the best results

### 📦 **Single Responsibility Principle**
- **.NET**: Data management and orchestration
- **Python**: AI and vector operations
- **Elasticsearch**: Text search
- **Pinecone**: Vector search
- **RabbitMQ**: Messaging

### 📈 **Scalability**
- Each service can be scaled independently
- Easy load distribution with RabbitMQ
- Horizontal scaling support

---

## 🚀 Getting Started

> **Note**: Refer to relevant documentation for detailed setup instructions and code examples.

### Requirements
- .NET 8.0+
- Python 3.9+
- Elasticsearch 8.x
- RabbitMQ 3.x
- Pinecone account

---

## 📝 Future Improvements

- [ ] RabbitMQ exchange type details
- [ ] RRF algorithm code implementation
- [ ] Performance optimizations
- [ ] Multi-language support improvements
- [ ] Caching strategies

---

## 📧 Contact

Feel free to open issues or submit pull requests with your questions.