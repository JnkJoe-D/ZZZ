using System;

namespace Game.Framework
{
    /// <summary>
    /// 协议编解码器
    ///
    /// 与服务端 GamePackage 格式完全匹配:
    ///   [Length:4字节 LE][MsgId:2字节 LE][Sequence:4字节 LE][Payload:变长]
    ///
    /// HeaderSize = 10 字节
    /// MaxPackageSize = 64KB
    /// </summary>
    public static class PacketCodec
    {
        /// <summary>头部固定大小：Length(4) + MsgId(2) + Sequence(4) = 10</summary>
        public const int HeaderSize = 10;

        /// <summary>最大包大小（与服务端一致）</summary>
        public const int MaxPackageSize = 64 * 1024;

        // ────────────────────────────────────────
        // 编码（发送时调用）
        // ────────────────────────────────────────

        /// <summary>
        /// 将消息编码为完整的二进制包
        /// </summary>
        /// <param name="msgId">消息 ID</param>
        /// <param name="sequence">序列号（递增，防重放）</param>
        /// <param name="payload">Protobuf 序列化后的消息体（可为 null 或空）</param>
        /// <returns>可直接发送的字节数组</returns>
        public static byte[] Encode(ushort msgId, uint sequence, byte[] payload)
        {
            int payloadLen = payload?.Length ?? 0;
            int totalLen   = HeaderSize + payloadLen;

            var buffer = new byte[totalLen];

            // Length (4字节, 小端) — 包含头部的总长度
            buffer[0] = (byte)(totalLen);
            buffer[1] = (byte)(totalLen >> 8);
            buffer[2] = (byte)(totalLen >> 16);
            buffer[3] = (byte)(totalLen >> 24);

            // MsgId (2字节, 小端)
            buffer[4] = (byte)(msgId);
            buffer[5] = (byte)(msgId >> 8);

            // Sequence (4字节, 小端)
            buffer[6] = (byte)(sequence);
            buffer[7] = (byte)(sequence >> 8);
            buffer[8] = (byte)(sequence >> 16);
            buffer[9] = (byte)(sequence >> 24);

            // Payload
            if (payloadLen > 0)
            {
                Buffer.BlockCopy(payload, 0, buffer, HeaderSize, payloadLen);
            }

            return buffer;
        }

        // ────────────────────────────────────────
        // 解码（接收时调用）
        // ────────────────────────────────────────

        /// <summary>
        /// 尝试从环形缓冲区中解码一个完整的包
        /// 处理粘包/拆包（TCP 场景）
        /// </summary>
        /// <param name="buffer">接收缓冲区</param>
        /// <param name="offset">当前读取起点（会被推进）</param>
        /// <param name="length">缓冲区中的可用数据长度（会被减少）</param>
        /// <param name="msgId">解码出的消息 ID</param>
        /// <param name="sequence">解码出的序列号</param>
        /// <param name="payload">解码出的消息体</param>
        /// <returns>是否成功解码出一个完整包</returns>
        public static bool TryDecode(
            byte[]      buffer,
            ref int     offset,
            ref int     length,
            out ushort  msgId,
            out uint    sequence,
            out byte[]  payload)
        {
            msgId    = 0;
            sequence = 0;
            payload  = null;

            // 数据不足头部长度，等待更多数据
            if (length < HeaderSize)
                return false;

            // 读取 Length（4字节，小端）
            int totalLen = buffer[offset]
                         | (buffer[offset + 1] << 8)
                         | (buffer[offset + 2] << 16)
                         | (buffer[offset + 3] << 24);

            // 安全校验
            if (totalLen < HeaderSize || totalLen > MaxPackageSize)
            {
                throw new Exception($"[PacketCodec] 非法包大小: {totalLen}，可能协议不匹配");
            }

            // 数据不足完整包长度，等待
            if (length < totalLen)
                return false;

            // 读取 MsgId（2字节，小端）
            msgId = (ushort)(buffer[offset + 4]
                           | (buffer[offset + 5] << 8));

            // 读取 Sequence（4字节，小端）
            sequence = (uint)(buffer[offset + 6]
                            | (buffer[offset + 7] << 8)
                            | (buffer[offset + 8] << 16)
                            | (buffer[offset + 9] << 24));

            // 读取 Payload
            int payloadLen = totalLen - HeaderSize;
            if (payloadLen > 0)
            {
                payload = new byte[payloadLen];
                Buffer.BlockCopy(buffer, offset + HeaderSize, payload, 0, payloadLen);
            }
            else
            {
                payload = Array.Empty<byte>();
            }

            // 推进游标
            offset += totalLen;
            length -= totalLen;

            return true;
        }

        /// <summary>
        /// UDP 解码 — 每个数据报正好一个完整包，无需处理粘包
        /// </summary>
        public static bool DecodeUdp(
            byte[]      data,
            int         dataLen,
            out ushort  msgId,
            out uint    sequence,
            out byte[]  payload)
        {
            msgId    = 0;
            sequence = 0;
            payload  = null;

            if (dataLen < HeaderSize)
                return false;

            // 读取 MsgId
            msgId = (ushort)(data[4] | (data[5] << 8));

            // 读取 Sequence
            sequence = (uint)(data[6] | (data[7] << 8) | (data[8] << 16) | (data[9] << 24));

            // 读取 Payload
            int payloadLen = dataLen - HeaderSize;
            if (payloadLen > 0)
            {
                payload = new byte[payloadLen];
                Buffer.BlockCopy(data, HeaderSize, payload, 0, payloadLen);
            }
            else
            {
                payload = Array.Empty<byte>();
            }

            return true;
        }
    }
}
