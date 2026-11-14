using System;

namespace ChartEditor
{
    /// <summary>
    /// ホールド(スペースホールド)のノーツが、役割ごとに「最後に使っていた種類」を覚えておく
    ///
    /// ホールドのノーツは、前後のつながりで役割(始点・中継点・終点)が決まり、役割ごとに選べる種類が違う
    /// (中継点なら HoldRelay か HoldMeshRelay、など)。つながりが変わって役割が変わると、種類もその役割のものに
    /// 付け替わる。このとき標準の種類に決め打ちすると、操作を元に戻して役割が戻っても、種類は標準のまま戻らない。
    /// 例: 始点 → メッシュ中継点 → 終点 のホールドで終点を消すと、中継点は終点になる。
    ///     元に戻すと中継点に戻るが、メッシュ中継点だったことが忘れられて、ふつうの中継点になってしまう。
    /// そこで、役割ごとに最後に使っていた種類を覚えておき、その役割に戻ったときは覚えておいた種類にする。
    /// </summary>
    public class ChainNoteTypeMemory
    {
        // 役割ごとの、選べる種類 (先頭が標準の種類)
        readonly DeploymentNoteType[] startTypes;
        readonly DeploymentNoteType[] relayTypes;
        readonly DeploymentNoteType[] endTypes;

        // 役割ごとの、最後に使っていた種類
        DeploymentNoteType startType;
        DeploymentNoteType relayType;
        DeploymentNoteType endType;

        /// <param name="startTypes">始点として選べる種類 (先頭が標準)</param>
        /// <param name="relayTypes">中継点として選べる種類 (先頭が標準)</param>
        /// <param name="endTypes">終点として選べる種類 (先頭が標準)</param>
        public ChainNoteTypeMemory(DeploymentNoteType[] startTypes, DeploymentNoteType[] relayTypes, DeploymentNoteType[] endTypes)
        {
            this.startTypes = startTypes;
            this.relayTypes = relayTypes;
            this.endTypes = endTypes;

            startType = startTypes[0];
            relayType = relayTypes[0];
            endType = endTypes[0];
        }

        /// <summary>
        /// ノーツの種類が決まった・変えられたときに呼ぶ。その種類を、対応する役割の「最後に使っていた種類」として覚える
        /// </summary>
        public void Remember(DeploymentNoteType noteType)
        {
            if (Array.IndexOf(startTypes, noteType) >= 0) { startType = noteType; }
            else if (Array.IndexOf(relayTypes, noteType) >= 0) { relayType = noteType; }
            else if (Array.IndexOf(endTypes, noteType) >= 0) { endType = noteType; }
        }

        /// <summary>
        /// 前後のつながりから決まる役割に合う種類を返す
        /// 今の種類がその役割で選べる種類なら、そのまま返す。合わないときは、その役割で最後に使っていた種類を返す
        /// </summary>
        /// <param name="hasBackNote">前にノーツがつながっているか</param>
        /// <param name="hasNextNote">後ろにノーツがつながっているか</param>
        /// <param name="currentType">今の種類</param>
        public DeploymentNoteType Resolve(bool hasBackNote, bool hasNextNote, DeploymentNoteType currentType)
        {
            // 前につながっていなければ始点 (どこにもつながっていないノーツも、始点として扱う)
            if (!hasBackNote) { return Array.IndexOf(startTypes, currentType) >= 0 ? currentType : startType; }

            // 前後につながっていれば中継点
            if (hasNextNote) { return Array.IndexOf(relayTypes, currentType) >= 0 ? currentType : relayType; }

            // 前だけにつながっていれば終点
            return Array.IndexOf(endTypes, currentType) >= 0 ? currentType : endType;
        }
    }
}
