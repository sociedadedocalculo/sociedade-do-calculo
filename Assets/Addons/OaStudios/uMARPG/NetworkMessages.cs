using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UMARPGCharacterCreateMsg : MessageBase {
    public static short MsgId = 4001;
    public string name;
    public int classIndex;
    public byte[] dnaValues;
    public bool gender;
    public byte hairIndex;
    public byte beardIndex;
    public byte skinColorIndex;
    public byte hairColorIndex;
}

public partial class CharactersAvailableMsg {

    public partial struct CharacterPreview {
        public byte[] dnaValues;
        public bool gender;
        public byte hairIndex;
        public byte beardIndex;
        public byte skinColorIndex;
        public byte hairColorIndex;
    }

    public void Load_UMARPG(List<Player> players) {
        for(int i = 0; i < players.Count; i++) {
            Player p = players[i];
            CharacterPreview preview = characters[i];

            preview.hairIndex = p.umaSyncData[0].value; // Hair Index
            preview.beardIndex = p.umaSyncData[1].value; // Beard Index
            preview.skinColorIndex = p.umaSyncData[2].value; // Skin Color Index
            preview.hairColorIndex = p.umaSyncData[3].value; // Hair Color Index
            preview.gender = p.gender;

            preview.dnaValues = new byte[p.dna.Count];
            for (int k = 0; k < p.dna.Count; k++)
                preview.dnaValues[k] = p.dna[k].value;

            // Structs are not OOP so we need this.
            characters[i] = preview;
        }
    }

}