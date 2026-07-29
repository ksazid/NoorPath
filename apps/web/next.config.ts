import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  poweredByHeader: false,
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: "/api/v1/:path*",
        destination: `${process.env.NOORPATH_API_URL ?? "http://localhost:5000"}/api/v1/:path*`,
      },
    ];
  },
};

export default nextConfig;
