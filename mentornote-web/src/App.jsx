import { useState } from "react";
import { motion } from "framer-motion";
import {
  Download,
  Mic,
  MessageSquare,
  Zap,
  Star,
  Send,
  Mail,
} from "lucide-react";
import "./App.css";

export default function App() {
  const [rating, setRating] = useState(0);

  return (
    <div className="page">
      {/* HERO */}
      <section className="hero">
        <motion.span
          className="pill"
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
        >
           Now in private beta
        </motion.span>

        <motion.h1
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6 }}
        >
          Mentor<span>note</span>
        </motion.h1>

        <motion.p
          className="subtitle"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.2 }}
        >
          Your real-time AI co-pilot for meetings
        </motion.p>


        <motion.button
          className="primary-btn"
          whileHover={{ scale: 1.05 }}
        >
          <Download size={18} />
          Download for Windows
        </motion.button>

          <motion.p
          className="sub-subtitle"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.2 }}
        >
          Private beta things may break
        </motion.p>
      </section>

      {/* FEATURES */}
      <section className="features">
        <h2>A quiet voice in your ear</h2>
          <p className="features-subtitle">
            MentorNote listens during your meetings and quietly shows short, relevant
            suggestions in real time—helping you stay confident, remember key points,
            and respond clearly.
          </p>
        <div className="feature-grid">
          {[
            {
              icon: Mic,
              title: "Listens quietly",
              text: "Runs in the background during calls without interrupting.",
            },
            {
              icon: MessageSquare,
              title: "Suggests in context",
              text: "Short prompts based on what’s being discussed.",
            },
            {
              icon: Zap,
              title: "Keeps you sharp",
              text: "Helps you phrase thoughts clearly and confidently.",
            },
          ].map((f, i) => (
            <motion.div
              key={i}
              className="card"
              initial={{ opacity: 0, y: 40 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.25 }}
            >
             <div className="icon-pill">
              <f.icon size={20} />
            </div>
            
              <h3>{f.title}</h3>
              <p>{f.text}</p>
            </motion.div>
          ))}
        </div>
          <div className="features-callout">
            Not a note-taker. MentorNote helps you <strong>during</strong> conversations,
            not after.
          </div>
      </section>
      {/* HOW IT WORKS */}
      <section className="how">
        <h2>How it works</h2>

        <div className="timeline">
          {[
            {
              title: "Launch before your meeting",
              desc: "MentorNote runs locally on your Windows machine. Start it when you're about to join a call.",
            },
            {
              title: "It listens as you talk",
              desc: "The app picks up the conversation through your microphone and processes it in real time.",
            },
            {
              title: "Get subtle prompts when needed",
              desc: "Short, contextual suggestions appear on screen—helping you stay on track without taking over.",
            },
          ].map((step, i) => (
            <motion.div
              key={i}
              className="timeline-item"
              initial={{ opacity: 0, x: -40 }}
              whileInView={{ opacity: 1, x: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.35 }}
            >
              <div className="timeline-marker">
                <span>{`0${i + 1}`}</span>
              </div>

              <div className="timeline-content">
                <h3>{step.title}</h3>
                <p>{step.desc}</p>
              </div>
            </motion.div>
          ))}
        </div>
      </section>

      {/* EARLY ACCESS */}
      <section className="early-access">
        <motion.div
          className="early-card"
          initial={{ opacity: 0, scale: 0.96 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={{ once: true }}
          transition={{ duration: 0.9, ease: "easeOut" }}
        >
          <h2>Early access</h2>

          <p>
            Mentornote is still in early development. Some things might not work
            perfectly but that’s where you come in. Your feedback helps shape
            what this becomes.
          </p>

          <button className="secondary-btn">
            <Download size={16} />
            Try the beta
          </button>
        </motion.div>
      </section>

      {/* FEEDBACK */}
<section className="feedback">
  <h2>Share your thoughts</h2>
  <p className="feedback-subtitle">
    Your feedback helps make MentorNote better. Let us know what's working,
    what's not, or what you'd like to see.
  </p>

  <div className="feedback-card">
    <div className="feedback-row">
      <input placeholder="Your name (optional)" />
      <input placeholder="Your email (optional)" />
    </div>

    <div className="rating-row">
      <span>Rate your experience (optional):</span>
      <div className="stars">
        {[1, 2, 3, 4, 5].map((s) => (
          <Star
            key={s}
            onClick={() => setRating(s)}
            className={s <= rating ? "star active" : "star"}
          />
        ))}
      </div>
    </div>

    <textarea placeholder="Tell us what you think..." />

    <button className="primary-btn full">
      <Send size={16} />
      Send feedback
    </button>
  </div>
</section>
    </div>
  );
}
