// Vercel Serverless Function: /api/chain
// Handles GET, POST, and OPTIONS for chain synchronization.

interface ChainData {
  prefix: string;
  healers: string[];
  interval: number;
  sender?: string;
  timestamp: number;
}

// In-memory fallback cache across warm lambda invocations
const memoryStore = (globalThis as unknown as { __rotatonator_chain_store?: Map<string, ChainData> })
  .__rotatonator_chain_store ??= new Map<string, ChainData>();

function getKvConfig() {
  // Check common environment variable names used by Vercel and Upstash
  let url =
    process.env.KV_REST_API_URL ||
    process.env.UPSTASH_REDIS_REST_URL;
  let token =
    process.env.KV_REST_API_TOKEN ||
    process.env.UPSTASH_REDIS_REST_TOKEN;

  // Fallback: search process.env for any keys ending in REST_URL / REST_TOKEN
  if (!url) {
    const urlKey = Object.keys(process.env).find(
      (k) => k.endsWith('_REST_API_URL') || k.endsWith('_REST_URL')
    );
    if (urlKey) url = process.env[urlKey];
  }

  if (!token) {
    const tokenKey = Object.keys(process.env).find(
      (k) => k.endsWith('_REST_API_TOKEN') || k.endsWith('_REST_TOKEN')
    );
    if (tokenKey) token = process.env[tokenKey];
  }

  if (url && token) {
    return { url: url.trim().replace(/\/$/, ''), token: token.trim() };
  }
  return null;
}

async function getChainFromKv(prefix: string): Promise<ChainData | null> {
  const kv = getKvConfig();
  if (!kv) return null;

  try {
    const key = `chain:${prefix.toUpperCase()}`;
    const response = await fetch(`${kv.url}/get/${encodeURIComponent(key)}`, {
      headers: {
        Authorization: `Bearer ${kv.token}`,
      },
    });

    if (!response.ok) return null;
    const result = await response.json();
    if (result && result.result) {
      return typeof result.result === 'string' ? JSON.parse(result.result) : result.result;
    }
  } catch (err) {
    console.error('[KV Error] Failed to get chain:', err);
  }
  return null;
}

async function saveChainToKv(prefix: string, data: ChainData): Promise<boolean> {
  const kv = getKvConfig();
  if (!kv) return false;

  try {
    const key = `chain:${prefix.toUpperCase()}`;
    // Using Upstash / Vercel KV REST command endpoint
    const response = await fetch(kv.url, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${kv.token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(['SET', key, JSON.stringify(data)]),
    });
    return response.ok;
  } catch (err) {
    console.error('[KV Error] Failed to save chain:', err);
    return false;
  }
}

export default async function handler(req: any, res: any) {
  // Set CORS headers
  res.setHeader('Access-Control-Allow-Credentials', 'true');
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET,OPTIONS,PATCH,DELETE,POST,PUT');
  res.setHeader(
    'Access-Control-Allow-Headers',
    'X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Content-Type, Date, X-Api-Version'
  );

  if (req.method === 'OPTIONS') {
    res.status(200).end();
    return;
  }

  if (req.method === 'GET') {
    const prefix = (req.query?.prefix as string)?.trim();
    if (!prefix) {
      const kv = getKvConfig();
      const detectedKeys = Object.keys(process.env).filter((k) =>
        /REST|UPSTASH|KV|REDIS/i.test(k)
      );
      return res.status(200).json({
        status: 'ok',
        storage: kv ? 'upstash_connected' : 'in_memory_fallback',
        kvUrlConfigured: Boolean(kv?.url),
        detectedStorageEnvVars: detectedKeys,
        usage: 'Use GET /api/chain?prefix=<prefix> to retrieve, or POST /api/chain to save.',
      });
    }

    const normPrefix = prefix.toUpperCase();

    // Try KV first if configured, else check in-memory store
    let chain = await getChainFromKv(normPrefix);
    if (!chain) {
      chain = memoryStore.get(normPrefix) ?? null;
    }

    if (!chain) {
      return res.status(404).json({
        success: false,
        message: `No active chain found for prefix '${prefix}'`,
      });
    }

    return res.status(200).json({
      success: true,
      data: chain,
    });
  }

  if (req.method === 'POST') {
    try {
      let body = req.body;
      if (typeof body === 'string') {
        body = JSON.parse(body);
      }

      if (!body) {
        return res.status(400).json({
          success: false,
          error: 'Missing request body',
        });
      }

      const prefix = typeof body.prefix === 'string' ? body.prefix.trim() : '';
      if (!prefix) {
        return res.status(400).json({
          success: false,
          error: 'Missing required field: prefix',
        });
      }

      if (!Array.isArray(body.healers) || body.healers.length === 0) {
        return res.status(400).json({
          success: false,
          error: 'Field healers must be a non-empty array of healer names',
        });
      }

      const interval = typeof body.interval === 'number' && body.interval > 0 ? body.interval : 6.0;
      const sender = typeof body.sender === 'string' ? body.sender.trim() : '';
      const timestamp = typeof body.timestamp === 'number' ? body.timestamp : Date.now();

      const chainData: ChainData = {
        prefix,
        healers: body.healers.map((h: any) => String(h).trim()).filter(Boolean),
        interval,
        sender,
        timestamp,
      };

      const normPrefix = prefix.toUpperCase();

      // Save to memory store
      memoryStore.set(normPrefix, chainData);

      // Save to KV if configured
      const kvSaved = await saveChainToKv(normPrefix, chainData);

      return res.status(200).json({
        success: true,
        prefix,
        timestamp,
        healersCount: chainData.healers.length,
        storage: kvSaved ? 'upstash' : 'in_memory',
      });
    } catch (err: any) {
      return res.status(400).json({
        success: false,
        error: `Invalid payload: ${err.message}`,
      });
    }
  }

  return res.status(405).json({
    success: false,
    error: `Method ${req.method} Not Allowed`,
  });
}
